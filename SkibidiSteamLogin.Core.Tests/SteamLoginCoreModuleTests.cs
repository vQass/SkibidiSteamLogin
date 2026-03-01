using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Configurations;
using SkibidiSteamLogin.Core.Models.Consts;

namespace SkibidiSteamLogin.Core.Tests
{
    public class SteamLoginCoreModuleTests
    {
        [Fact]
        public void AddSteamLoginCoreModule_RegistersLoginHandler()
        {
            var services = new ServiceCollection();

            services.AddSteamLoginCoreModule();

            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ILoginHandler));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddSteamLoginCoreModule_RegistersCookiePersistenceService()
        {
            var services = new ServiceCollection();

            services.AddSteamLoginCoreModule();

            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICookiePersistenceService));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddSteamLoginCoreModule_RegistersHttpClientWrapper()
        {
            var services = new ServiceCollection();

            services.AddSteamLoginCoreModule();

            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHttpClientWrapper));
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void AddSteamLoginCoreModule_DefaultConfig_UsesAllSteamDomains()
        {
            var services = new ServiceCollection();
            services.AddSteamLoginCoreModule();

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<SkibidiLoginConfiguration>>();

            Assert.NotNull(options.Value.SetTokenDomains);
            Assert.Equal(SteamDomains.All.Count, options.Value.SetTokenDomains.Count);
            foreach (var domain in SteamDomains.All)
            {
                Assert.Contains(domain, options.Value.SetTokenDomains);
            }
        }

        [Fact]
        public void AddSteamLoginCoreModule_CustomConfig_AppliesCustomDomains()
        {
            var services = new ServiceCollection();
            services.AddSteamLoginCoreModule(opt =>
            {
                opt.SetTokenDomains = ["custom.domain.com"];
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<SkibidiLoginConfiguration>>();

            Assert.Single(options.Value.SetTokenDomains);
            Assert.Contains("custom.domain.com", options.Value.SetTokenDomains);
        }

        [Fact]
        public void AddSteamLoginCoreModule_CalledTwice_DoesNotDuplicateServiceRegistrations()
        {
            var services = new ServiceCollection();

            services.AddSteamLoginCoreModule();
            services.AddSteamLoginCoreModule();

            var loginHandlers = services.Where(s => s.ServiceType == typeof(ILoginHandler)).ToList();
            Assert.Single(loginHandlers);

            var httpWrappers = services.Where(s => s.ServiceType == typeof(IHttpClientWrapper)).ToList();
            Assert.Single(httpWrappers);

            var cookieServices = services.Where(s => s.ServiceType == typeof(ICookiePersistenceService)).ToList();
            Assert.Single(cookieServices);
        }

        [Fact]
        public void AddSteamLoginCoreModule_ReturnsServiceCollectionForChaining()
        {
            var services = new ServiceCollection();

            var result = services.AddSteamLoginCoreModule();

            Assert.Same(services, result);
        }
    }
}
