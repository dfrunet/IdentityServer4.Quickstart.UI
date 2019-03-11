using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{

    public class IdentityServerClient:Client
    {
        public string RedirectUri
        {
            get => RedirectUris?.FirstOrDefault();

            set
            {
                RedirectUris = new List<string>() {value};
                if (FrontChannelLogoutSessionRequired)
                    FrontChannelLogoutUri = $"{value}?oidcsignout=";
            }
        }
    }
}
