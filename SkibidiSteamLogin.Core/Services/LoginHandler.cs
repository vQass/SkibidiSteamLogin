using System.Net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Helpers;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Mapping;
using SkibidiSteamLogin.Core.Models.Configurations;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.Core.Services
{
    internal class LoginHandler : ILoginHandler
    {
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly SkibidiLoginConfiguration _options;
        private LoginResult _response;

        public LoginHandler(IHttpClientWrapper httpClientWrapper, IOptions<SkibidiLoginConfiguration> options)
        {
            _httpClientWrapper = httpClientWrapper;
            _options = options.Value;
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var sessionResult = await _httpClientWrapper.StartSessionAsync();

            if (!sessionResult.IsSuccess)
            {
                // TODO result model for login process

                return null;
            }

            var rsaResult = await _httpClientWrapper.GetRsaDataAsync(username);

            if (!rsaResult.IsSuccess)
            {
                // TODO result model for login process

                return null;
            }
            var encryptedPassword = EncryptionHelper.EncryptPassword(rsaResult.Data, password);

            var loginResult = await _httpClientWrapper.LoginAsync(username, encryptedPassword, rsaResult.Data.Timestamp);

            _response = loginResult.Data.ToLoginResult();

            return _response;
        }

        public async Task<LoginResult> EnterSteamGuardCodeAsync(string authcode, AuthGuardTypeEnum guardTypeEnum)
        {
            var steamGuardRequest = new SteamGuardRequest
            {
                ClientId = _response.ClientId,
                SteamId = _response.SteamId,
                Code = authcode,
                CodeType = guardTypeEnum
            };

            var result = await _httpClientWrapper.EnterSteamGuardCodeAsync(steamGuardRequest);

            await Task.Delay(500);

            var result2 = await _httpClientWrapper.PollAuthSessionStatusAsync(_response.ClientId, _response.RequestId);

            var result3 = await _httpClientWrapper.FinalizeLoginAsync(result2.Data);

            var tokensToSet = result3.Data.TransferInfo.Where(x => _options.SetTokenDomains.Any(y => x.Url.Contains(y)));

            foreach (var token in tokensToSet)
            {
                var result4 = await _httpClientWrapper.SetToken(_response.SteamId, token.Params.Auth, token.Params.Nonce, token.Url);
            }

            return null;
        }

        public CookieCollection GetCookies()
        {
            return _httpClientWrapper.GetCookies();
        }

        public void SaveCookies()

        {
            var cookies = _httpClientWrapper.GetCookies();

            var serialized = JsonConvert.SerializeObject(cookies);

            File.WriteAllText("cookies.json", serialized);
        }
        

        public void LoadCookies()
        {
            var data = File.ReadAllText("cookies.json");

            var cookies = JsonConvert.DeserializeObject<CookieCollection>(data);

            _httpClientWrapper.SetCookies(cookies);
        }
    }
}
