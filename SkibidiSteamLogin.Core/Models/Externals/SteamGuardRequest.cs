using SkibidiSteamLogin.Core.Enums;

namespace SkibidiSteamLogin.Core.Models.Externals
{
    public class SteamGuardRequest
    {
        public string ClientId { get; set; }
        public string SteamId { get; set; }
        public string Code { get; set; }
        public AuthGuardTypeEnum CodeType { get; set; }
    }
}
