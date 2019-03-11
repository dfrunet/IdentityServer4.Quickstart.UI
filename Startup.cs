using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Identity.Service;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdSrvHost.Configuration.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Wemore.Identity.Service;

namespace IdSrvHost
{
    public static class Constants
    {

        public const string IdpName = "SampleId";

        public const string TenantInstanceServerNameCookieName = "TenantInstanceServerName";

        public const string TenantIdClaimType = "tenantid";

        public const string SupportedModuleClaimType = "module";

        public const string RolesScopeType = "roles";

        public const string RedisConnectionStringKey = "RedisConnectionString";

        public const string TenantServiceUri = "TenantServiceUri";

        public const string AccessServiceUriKey = "AccessServiceUri";

        public const string ConfigApiName = "config-api";

        public static CultureInfo[] SupportedCultures = new[]
        {
            new CultureInfo("en-GB"),
            new CultureInfo("en"),
            new CultureInfo("sv"),
            new CultureInfo("nb"),
            new CultureInfo("nn"),
            new CultureInfo("fi"),
            new CultureInfo("da")
        };

    }

    public class Startup
    {
        private static readonly IdentityServerClients identityServerClients = new IdentityServerClients();
        public static IConfigurationRoot Config { get; set; }

        private ILogger<Startup> Logger { get; set; }



        public Startup(IHostingEnvironment env)
        {
            var host = new WebHostBuilder();
            var envVar = host.GetSetting("environment");
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{envVar.ToLower()}.json", false)
                .AddEnvironmentVariables();
            Config = builder.Build();

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRoot>(Config);

            Logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<Startup>();


            
            Config.GetSection(nameof(IdentityServerClients)).Bind(identityServerClients);
            services.AddSingleton(identityServerClients);

            //services.AddSingleton<UserStore>();
            //var cert = new X509Certificate2(Path.Combine(".", "Certs", "identityserver.pfx"), "password-rules-2019");
            services.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    //options.Authentication.
                })
                .AddInMemoryClients(identityServerClients.Clients)
                .AddInMemoryApiResources(GetApiResources())
                //.AddSigningCredential(cert)
                .AddInMemoryIdentityResources(GetIdentityResources())
                //.AddProfileService<UserProfileService>()
                .AddExtensionGrantValidator<DelegationGrantValidator>()

                .AddDeveloperSigningCredential()
                //.AddJwtBearerClientAuthentication()
                .AddAppAuthRedirectUriValidator()
                .AddTestUsers(TestUsers.Users);


            services.AddAuthentication()
                .AddLocalAccessTokenValidation("token", isAuth => { });

            var redis = ConnectionMultiplexer.Connect(Config[Constants.RedisConnectionStringKey]);

            services.AddStackExchangeRedisCache(config =>
            {
                config.Configuration = Config[Constants.RedisConnectionStringKey];
            });

            services.AddScoped<IUserSession, ExtendedUserSession>();

            //services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureInternalCookieOptionsEx>();

            services.AddSingleton<IPersistedGrantStore, RedisPersistedGrantStore>().Configure(
                (Action<PersistedGrantStoreOptions>)(options =>
                   options.DatabaseFactory = () =>
                       redis.GetDatabase(-1, null)));
            //services.AddSingleton<ITenantService, TenantService>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");


            // configures the OpenIdConnect handlers to persist the state parameter into the server-side IDistributedCache.
            services.AddOidcStateDataFormatterCache(Constants.IdpName);



            //services.AddTransient<ICustomAuthorizeRequestValidator, CustomAuthorizeRequestValidator>();

            //services.AddCsp();
            services.AddCors();
            services.AddMvc(/*options => options.EnableEndpointRouting = false*/);
                //.SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            services.AddDataProtection()
                .SetApplicationName(typeof(Startup).Namespace)
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
                //.ProtectKeysWithProvidedCertificate(cert)
                ;

            services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");


            return services.BuildServiceProvider(validateScopes: true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            app.UseMiddleware<Host.Logging.RequestLoggerMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-GB"),
                // Formatting numbers, dates, etc.
                SupportedCultures = Constants.SupportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = Constants.SupportedCultures
            });
            app.UseIdentityServer();

            app.UseMvcWithDefaultRoute();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("We should not be here");
            });
        }



        private Task OnTicketReceived(TicketReceivedContext context)
        {
            var identity = context.Principal.Identity as ClaimsIdentity;

            StringBuilder builder = new StringBuilder();
            var claims = identity?.Claims.Select(x => $"{x.Type}:{x.Value};");
            if (claims != null)
                builder.AppendJoin(", ", claims);
            Logger.LogInformation($"Ticket received: [Claims:{builder}]");
            //identity?.AddClaim(new Claim(AlfClaimTypes.SelectedTenant, AuthenticationOptions.TenantId));
            return Task.CompletedTask;
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource
                {
                    Name = Constants.ConfigApiName,
                    DisplayName = "Config API",
                    Scopes =
                    {
                        new Scope(Constants.ConfigApiName),
                        new Scope("config-api.admin")
                    }
                },
                new ApiResource
                {
                    Name = "graph-api",
                    Scopes =
                    {
                        new Scope
                        {
                            Name = "graph-api",
                            UserClaims =
                            {
                                JwtClaimTypes.SessionId,
                                JwtClaimTypes.Role,
                                Constants.TenantIdClaimType,
                                JwtClaimTypes.Email,
                                JwtClaimTypes.Locale,
                                Constants.SupportedModuleClaimType
                            }
                        },
                        new Scope("graph-api.backend"),
                        new Scope("external-callback")
                    }
                }
            };
        }

        public static List<IdentityResource> GetIdentityResources()
        {
            // Claims automatically included in OpenId scope
            var openIdScope = new IdentityResources.OpenId();
            openIdScope.UserClaims.Add(JwtClaimTypes.Locale);

            // Available scopes
            return new List<IdentityResource>
            {
                openIdScope,
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource(Constants.RolesScopeType, Constants.RolesScopeType,
                    new List<string> {JwtClaimTypes.Role, Constants.TenantIdClaimType})
                {
                    Required = true
                }
            };
        }
    }
}
