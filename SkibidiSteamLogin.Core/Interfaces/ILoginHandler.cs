using System.Net;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.Core.Interfaces
{
    public interface ILoginHandler
    {
        Task<OperationResult<LoginResult>> LoginAsync(string username, string password);
        Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(LoginResult loginSession, string authCode, AuthGuardType guardType);

        CookieCollection GetCookies();
        void SetCookies(CookieCollection cookies);
    }
}
