using SkibidiSteamLogin.Core.Models.Externals;
using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Mapping
{
    internal static class LoginResultMapping
    {
        internal static LoginResult ToLoginResult(this SteamLoginResponse loginResponse)
        {
            return new LoginResult
            {
                ClientId = loginResponse.ClientId,
                SteamId = loginResponse.SteamId,
                RequestId = loginResponse.RequestId,
                AllowedConfirmation = loginResponse.AllowedConfirmations
            };
        }
    }
}
