using System.Collections.Generic;
using System.Linq;
using IdentityServer4;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class ResourceOwnerClient : IdentityServerClient
    {
        public string ClientSecret
        {
            get => ClientSecrets.FirstOrDefault()?.Value;
            set
            {
                var secret = new Secret { Value = value.Sha512() };
                ClientSecrets = new[] { secret };
            }
        }

        public ResourceOwnerClient()
        {
            RequireClientSecret = true;
            Enabled = true;
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword;
            AlwaysIncludeUserClaimsInIdToken = true;
            IdentityTokenLifetime = 3600; // 1 Hour
            AccessTokenLifetime = 3600;
            RequireConsent = false;
            AllowRememberConsent = false;
            AllowedScopes = new List<string>
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                Constants.RolesScopeType,
                "graph-api"
            };
            AccessTokenType = AccessTokenType.Jwt;
            FrontChannelLogoutSessionRequired = false;
        }
    }
}
