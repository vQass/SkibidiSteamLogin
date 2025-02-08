using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Configurations;
using SkibidiSteamLogin.Core.Models.Consts;
using SkibidiSteamLogin.Core.Services;
using SkibidiSteamLogin.Core.Wrappers;

namespace SkibidiSteamLogin.Core
{
    public static class SteamLoginCoreModule
    {
        public static IServiceCollection AddSteamLoginCoreModule(this IServiceCollection services, Action<SkibidiLoginConfiguration> options = null)
        {
            services.TryAddSingleton<ILoginHandler, LoginHandler>();
            services.TryAddSingleton<IHttpClientWrapper, HttpClientWrapper>();

            if (options is null)
            {
                options = (opt) => { opt.SetTokenDomains = SetTokenDomains.AllDomains; };
            }

            services.Configure(options);

            return services;
        }
    }
}
