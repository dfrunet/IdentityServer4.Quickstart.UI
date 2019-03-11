using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class NativeAppClient: IdentityServerClient
    {
        public NativeAppClient()
        {
            //ClientId = "native-app-client";
            //ClientName = "Authorization Code Client";
            RequireClientSecret = false;
            Enabled = true;
            AllowedGrantTypes = GrantTypes.Code;
            AlwaysIncludeUserClaimsInIdToken = true;
            IdentityTokenLifetime = 3600; // 1 Hour
            AccessTokenLifetime = 3600;
            RequireConsent = false;
            RequirePkce = true;
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
            AllowOfflineAccess = true;
            RefreshTokenUsage = TokenUsage.OneTimeOnly;
            FrontChannelLogoutSessionRequired = false;
        }
    }
}
