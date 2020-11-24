using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Services;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Wemore.Identity.Service
{

    public class ExtendedUserSession : DefaultUserSession, IUserSession
    {
        private readonly IDistributedCache _cache;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;


        public ExtendedUserSession(IHttpContextAccessor httpContextAccessor, IAuthenticationSchemeProvider schemes,
            IAuthenticationHandlerProvider handlers, IdentityServerOptions options, ISystemClock clock,
            ILogger<IUserSession> logger, IDistributedCache redisCache) : base(httpContextAccessor,
            handlers, options, clock, logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _cache = redisCache;
        }


        /// <summary>
        /// Removes the session identifier cookie.
        /// </summary>
        /// <returns></returns>
        public override Task RemoveSessionIdCookieAsync()
        {
            _cache.Remove(GetKey());
            return base.RemoveSessionIdCookieAsync();
        }

        async Task IUserSession.AddClientIdAsync(string clientId)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            var clients = (await GetClientListAsync()).ToList();
            if (!clients.Contains(clientId))
            {
                clients.Add(clientId);
                _cache.SetString(GetKey(), JsonConvert.SerializeObject(clients),
                    new DistributedCacheEntryOptions() {AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)});
                await AddClientIdAsync(clientId);
            }
        }

       

        private string GetKey()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(c => c.Type == "sub") + "|clients";
        }

        async Task<IEnumerable<string>> IUserSession.GetClientListAsync()
        {
            var list = (await base.GetClientListAsync()).ToList();
            try
            {
                var cached = _cache.GetString(GetKey());
                if (cached != null)
                {
                    IEnumerable<string> cachedList = JsonConvert.DeserializeObject<IEnumerable<string>>(cached);
                    list = list.Union(cachedList).ToList();
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading client list from cache: {0}", ex.Message);
                try
                {
                    _cache.Remove(GetKey());
                }
                catch (Exception ex1)
                {
                    _logger.LogError("Error clearing cached clients list: {0}", ex1);
                }
            }

            return Enumerable.Empty<string>();
        }

    }

}

