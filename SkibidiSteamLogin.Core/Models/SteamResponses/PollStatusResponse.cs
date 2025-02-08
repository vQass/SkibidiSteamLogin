using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class PollStatusResponse
    {
        [JsonProperty("refresh_token")]
        internal string RefreshToken { get; set; }

        [JsonProperty("access_token")]
        internal string AccessToken { get; set; }

        [JsonProperty("had_remote_interaction")]
        internal bool HadRemoteInteraction { get; set; }

        [JsonProperty("account_name")]
        internal string AccountName { get; set; }
    }
}
