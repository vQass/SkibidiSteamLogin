using System.Net;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.Core.Interfaces
{
    public interface ILoginHandler
    {
        Task<LoginResult> LoginAsync(string username, string password);
        Task<LoginResult> EnterSteamGuardCodeAsync(string authcode, AuthGuardTypeEnum guardTypeEnum);

        CookieCollection GetCookies();

        void SaveCookies();
        void LoadCookies();
    }
}
