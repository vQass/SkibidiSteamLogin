using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Models.Externals
{
    public class LoginResult
    {
        public string ClientId { get; set; }
        public string SteamId { get; set; }
        public string RequestId { get; set; }
        public List<AllowedConfirmation> AllowedConfirmation { get; set; }
    }
}
