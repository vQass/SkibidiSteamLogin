using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class FinalizeLoginResult
    {
        [JsonProperty("steamID")]
        internal string SteamID { get; set; }

        [JsonProperty("redir")]
        internal string Redir { get; set; }

        [JsonProperty("transfer_info")]
        internal List<TransferInfo> TransferInfo { get; set; }

        [JsonProperty("primary_domain")]
        internal string PrimaryDomain { get; set; }
    }

    internal class Params
    {
        [JsonProperty("nonce")]
        internal string Nonce { get; set; }

        [JsonProperty("auth")]
        internal string Auth { get; set; }
    }

    internal class TransferInfo
    {
        [JsonProperty("url")]
        internal string Url { get; set; }

        [JsonProperty("params")]
        internal Params Params { get; set; }
    }
}
