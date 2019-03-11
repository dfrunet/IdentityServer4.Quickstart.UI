using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class WebAppClient : IdentityServerClient
    {
        public string SubscriptionModule { get; set; }

        public WebAppClient()
        {
            //ClientId = "spa-client";
            RequireConsent = false;
            Enabled = true;
            AllowedGrantTypes = GrantTypes.Implicit;
            AccessTokenType = AccessTokenType.Jwt;
            AlwaysIncludeUserClaimsInIdToken = true;
            UpdateAccessTokenClaimsOnRefresh = true;
            IdentityTokenLifetime = 60 * 60 * 12;//12 hours
            AccessTokenLifetime = 60 * 60 * 12;//12 hours
            AllowedScopes = new List<string>
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                Constants.RolesScopeType,
                "graph-api"
            };
            AllowAccessTokensViaBrowser = true;
            FrontChannelLogoutSessionRequired = true;
        }
    }
}
