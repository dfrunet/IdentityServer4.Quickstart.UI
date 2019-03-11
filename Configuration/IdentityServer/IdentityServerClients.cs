using System.Collections.Generic;
using IdentityServer4.Models;

namespace IdSrvHost.Configuration.IdentityServer
{
    public class IdentityServerClients
    {
        public NativeAppClient[] NativeAppClients { get; set; }
        public WebAppClient[] WebAppClients { get; set; }
        public ServiceClient[] ServiceClients { get; set; }
        public ResourceOwnerClient[] ResourceOwnerClients { get; set; }

        public IEnumerable<Client> Clients
        {
            get
            {
                var clients = new List<Client>(NativeAppClients?.Length ?? 1 + WebAppClients?.Length ?? 1 + ServiceClients?.Length ?? 1 + ResourceOwnerClients?.Length ?? 1);
                if (NativeAppClients != null)
                    clients.AddRange(NativeAppClients);
                if (WebAppClients != null)
                    clients.AddRange(WebAppClients);
                if (ServiceClients != null)
                    clients.AddRange(ServiceClients);
                if (ResourceOwnerClients != null)
                    clients.AddRange(ResourceOwnerClients);
                return clients;
            }
        }
    }
}
