using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Identity.Service
{
    public class PersistedGrantStoreOptions
    {
        public Func<IDatabase> DatabaseFactory { get; set; }
    }

    public class RedisPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IDatabase _database;

        public RedisPersistedGrantStore(
            ILogger<IPersistedGrantStore> logger, IOptions<PersistedGrantStoreOptions> persistedGrantStoreOptions,
            IConfiguration configuration)
        {
            _logger = logger;
            _database = persistedGrantStoreOptions.Value.DatabaseFactory();
            _configuration = configuration;
        }

        private static string GetKey(string key) => key;

        private static string GetSetKey(string subjectId) => subjectId;

        private static string GetSetKey(string subjectId, string clientId) => $"{subjectId}:{clientId}";

        private static string GetSetKey(string subjectId, string clientId, string type) =>
            $"{subjectId}:{clientId}:{type}";

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
                throw new ArgumentNullException(nameof(grant));
            try
            {
                var data = ConvertToJson(grant);
                var grantKey = GetKey(grant.Key);
                var expiresIn = grant.Expiration - DateTime.UtcNow;
                if (!string.IsNullOrEmpty(grant.SubjectId))
                {
                    var setKey = GetSetKey(grant.SubjectId, grant.ClientId, grant.Type);
                    var transaction = this._database.CreateTransaction();
#pragma warning disable 4014
                    transaction.StringSetAsync(grantKey, data, expiresIn);
                    transaction.SetAddAsync(GetSetKey(grant.SubjectId), grantKey);
                    transaction.SetAddAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
                    transaction.SetAddAsync(setKey, grantKey);
                    transaction.KeyExpireAsync(setKey, expiresIn);
#pragma warning restore 4014
                    await transaction.ExecuteAsync();
                }
                else
                {
                    await this._database.StringSetAsync(grantKey, data, expiresIn);
                }
                _logger.LogDebug(
                    $"grant for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} persisted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"exception storing persisted grant to Redis database for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} : {ex.Message}");
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var data = await this._database.StringGetAsync(GetKey(key));
            _logger.LogDebug($"{key} found in database: {data.HasValue}");
            return data.HasValue ? ConvertFromJson(data) : null;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var setKey = GetSetKey(subjectId);
            var grantsKeys = await this._database.SetMembersAsync(setKey);
            if (grantsKeys.Count() == 0)
                return Enumerable.Empty<PersistedGrant>();
            var grants = await this._database.StringGetAsync(grantsKeys.Select(_ => (RedisKey) _.ToString()).ToArray());
            var keysToDelete = grantsKeys
                .Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
                .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
            if (keysToDelete.Count() != 0)
                await this._database.SetRemoveAsync(setKey, keysToDelete.ToArray());
            _logger.LogDebug($"{grantsKeys.Count(_ => _.HasValue)} persisted grants found for {subjectId}");
            return grants.Where(_ => _.HasValue).Select(_ => ConvertFromJson(_));
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var grant = await this.GetAsync(key);
                if (grant == null)
                {
                    _logger.LogDebug($"no {key} persisted grant found in database");
                    return;
                }
                var grantKey = GetKey(key);
                _logger.LogDebug($"removing {key} persisted grant from database");
                var transaction = this._database.CreateTransaction();
#pragma warning disable 4014
                transaction.KeyDeleteAsync(grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId, grant.Type), grantKey);
#pragma warning restore 4014
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"exception removing {key} persisted grant from database: {ex.Message}");
            }
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            if (filter.ClientId != null && filter.SubjectId != null && filter.Type!=null)
                await RemoveAllAsync(filter.SubjectId, filter.ClientId, filter.Type);
            else if(filter.ClientId != null && filter.SubjectId != null)
                await RemoveAllAsync(filter.SubjectId, filter.ClientId);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId);
                var grantsKeys = await this._database.SetMembersAsync(setKey);
                _logger.LogDebug(
                    $"removing {grantsKeys.Count()} persisted grants from database for subject {subjectId}, clientId {clientId}");
                if (grantsKeys.Count() == 0) return;
                var transaction = this._database.CreateTransaction();
#pragma warning disable 4014
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey) _.ToString())
                    .Concat(new RedisKey[] {setKey}).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
#pragma warning restore 4014
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    $"removing persisted grants from database for subject {subjectId}, clientId {clientId}: {ex.Message}");
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId, type);
                var grantsKeys = await this._database.SetMembersAsync(setKey);
                _logger.LogDebug(
                    $"removing {grantsKeys.Count()} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}");
                if (grantsKeys.Count() == 0) return;
                var transaction = this._database.CreateTransaction();
#pragma warning disable 4014
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey) _.ToString())
                    .Concat(new RedisKey[] {setKey}).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId, clientId), grantsKeys);
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
#pragma warning restore 4014
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    $"exception removing persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}: {ex.Message}");
            }
        }

        #region Json

        private static string ConvertToJson(PersistedGrant grant)
        {
            return JsonConvert.SerializeObject(grant);
        }

        private static PersistedGrant ConvertFromJson(string data)
        {
            return JsonConvert.DeserializeObject<PersistedGrant>(data);
        }

        #endregion
    }
}