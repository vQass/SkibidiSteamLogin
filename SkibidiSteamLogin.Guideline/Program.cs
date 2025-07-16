using SkibidiSteamLogin.Core;

namespace SkibidiSteamLogin.Guideline
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddSteamLoginCoreModule(options => 
            {
                options.SetTokenDomains = [Core.Models.Consts.SetTokenDomains.SteamCommunityDomain]; // choose domains for token setting
            });

            var host = builder.Build();
            host.Run();
        }
    }
}