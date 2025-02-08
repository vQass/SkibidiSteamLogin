namespace SkibidiSteamLogin.Core.Models.Consts
{
    internal class Endpoints
    {
        internal const string SteamCommunityUrlBase = "https://steamcommunity.com/";
        internal const string SteamPoweredUrlBase = "https://api.steampowered.com/";
        internal const string SteamLoginUrlBase = "https://login.steampowered.com/";

        internal const string GetRsa = "IAuthenticationService/GetPasswordRSAPublicKey/v1/";
        internal const string CredentialsSessionStart = "IAuthenticationService/BeginAuthSessionViaCredentials/v1/";
        internal const string CredentialsSessionUpdateWithGuardCode = "IAuthenticationService/UpdateAuthSessionWithSteamGuardCode/v1/";
        internal const string PollAuthSessionStatus = "IAuthenticationService/PollAuthSessionStatus/v1/";
        internal const string FinalizeLogin = "jwt/finalizelogin";
    }
}
