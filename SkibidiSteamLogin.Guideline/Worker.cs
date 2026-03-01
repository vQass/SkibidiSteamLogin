using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Interfaces;
using System.Net;

namespace SkibidiSteamLogin.Guideline
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoginHandler _loginHandler;

        public Worker(ILogger<Worker> logger, ILoginHandler loginHandler)
        {
            _logger = logger;
            _loginHandler = loginHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var result = await _loginHandler.LoginAsync("username", "password");

            var result2 = await _loginHandler.EnterSteamGuardCodeAsync(result.Data, "", AuthGuardType.None); // if you don't have a guard code, you can use an empty string (run it to finalize login process)
            var result3 = await _loginHandler.EnterSteamGuardCodeAsync(result.Data, "authcode", AuthGuardType.EmailCode); // otherwise, you can use your authcode and guard type

            var cookies = _loginHandler.GetCookies(); // use this to get cookies after login
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(cookies);

            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler))
            {
                try
                {
                    var result4 = await client.GetAsync("");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error");
                }
            }
        }
    }
}
