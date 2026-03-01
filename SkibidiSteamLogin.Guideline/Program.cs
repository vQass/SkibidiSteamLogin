using SkibidiSteamLogin.Core;
using SkibidiSteamLogin.Core.Models.Consts;

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
                options.SetTokenDomains = [SteamDomains.Community]; // choose domains for token setting
            });

            var host = builder.Build();
            host.Run();
        }
    }
}