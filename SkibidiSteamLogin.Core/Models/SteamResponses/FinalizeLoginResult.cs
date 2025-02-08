using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    public class FinalizeLoginResult
    {
        [JsonProperty("steamID")]
        public string SteamID { get; set; }

        [JsonProperty("redir")]
        public string Redir { get; set; }

        [JsonProperty("transfer_info")]
        public List<TransferInfo> TransferInfo { get; set; }

        [JsonProperty("primary_domain")]
        public string PrimaryDomain { get; set; }
    }

    public class Params
    {
        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("auth")]
        public string Auth { get; set; }
    }


    public class TransferInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("params")]
        public Params Params { get; set; }
    }

}
