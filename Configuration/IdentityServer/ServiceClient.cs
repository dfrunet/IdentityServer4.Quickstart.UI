using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class ServiceClient : IdentityServerClient
    {

        public string ClientSecret
        {
            get => ClientSecrets.FirstOrDefault()?.Value;
            set
            {
                var secret = new Secret {Value = value.Sha512()};
                ClientSecrets = new[] {secret};
            }
        }

        public new ICollection<string> AllowedGrantTypes
        {
            get
            {
                if (base.AllowedGrantTypes.Count == 0)
                    base.AllowedGrantTypes = GrantTypes.ClientCredentials;
                return
                    base.AllowedGrantTypes; //()? base.AllowedGrantTypes: new List<string>(GrantTypes.ClientCredentials);
            }

            set
            {
                base.AllowedGrantTypes = value;
            }
        }

        public ServiceClient()
        {
            RequireConsent = false;
            Enabled = true;
            //AllowedGrantTypes = GrantTypes.ClientCredentials;
            AccessTokenType = AccessTokenType.Jwt;
            AlwaysIncludeUserClaimsInIdToken = false;
            UpdateAccessTokenClaimsOnRefresh = true;
        }
    }
}
