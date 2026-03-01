using System.Net;

namespace SkibidiSteamLogin.Core.Interfaces
{
    public interface ICookiePersistenceService
    {
        Task SaveCookiesAsync(CookieCollection cookies);
        Task<CookieCollection> LoadCookiesAsync();
    }
}
