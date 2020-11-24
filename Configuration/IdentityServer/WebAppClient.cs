using System.Collections.Generic;
using System.Linq;
using IdentityServer4;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class WebAppClient : IdentityServerClient
    {
        public string SubscriptionModule { get; set; }



        public string ClientSecret
        {
            get => ClientSecrets.FirstOrDefault()?.Value;
            set
            {
                var secret = new Secret { Value = value.Sha512() };
                ClientSecrets = new[] { secret };
            }
        }


        public new ICollection<string> AllowedGrantTypes
        {
            get
            {
                //if (base.AllowedGrantTypes.Count == 0)
                //    base.AllowedGrantTypes = GrantTypes.Implicit;
                return
                    base.AllowedGrantTypes; //

            }

            set
            {
                if (base.AllowedGrantTypes.Count == 0 && value.Count == 0)
                {
                    base.AllowedGrantTypes = GrantTypes.Implicit;
                }
                else
                {
                    base.AllowedGrantTypes = value;
                }
            }
        }

        public WebAppClient()
        {
            //ClientId = "spa-client";
            RequireConsent = false;
            RequireClientSecret = false;
            Enabled = true;
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
