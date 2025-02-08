using System.Net;
using SkibidiSteamLogin.Core.Models.Externals;
using SkibidiSteamLogin.Core.Models.Internals;
using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Interfaces
{
    internal interface IHttpClientWrapper
    {
        Task<HttpResult> StartSessionAsync();
        Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username);
        Task<HttpDataResult<SteamLoginResponse>> LoginAsync(string username, string encryptedPassword, long timestamp);
        Task<HttpResult> EnterSteamGuardCodeAsync(SteamGuardRequest steamGuardRequest);
        Task<HttpDataResult<string>> PollAuthSessionStatusAsync(string clientId, string requestId);
        Task<HttpDataResult<FinalizeLoginResult>> FinalizeLoginAsync(string token);
        Task<HttpResult> SetToken(string steamId, string auth, string nonce, string url);
        CookieCollection GetCookies();
        void SetCookies(CookieCollection cookieCollection);
    }
}
