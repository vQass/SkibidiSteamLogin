using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Enums;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class SteamLoginResponse
    {
        [JsonProperty("client_id")]
        internal string ClientId { get; set; }

        [JsonProperty("request_id")]
        internal string RequestId { get; set; }

        [JsonProperty("interval")]
        internal int Interval { get; set; }

        [JsonProperty("allowed_confirmations")]
        internal List<AllowedConfirmation> AllowedConfirmations { get; set; }

        [JsonProperty("steamid")]
        internal string SteamId { get; set; }

        [JsonProperty("weak_token")]
        internal string WeakToken { get; set; }

        [JsonProperty("extended_error_message")]
        internal string ExtendedErrorMessage { get; set; }
    }

    public class AllowedConfirmation
    {
        [JsonProperty("confirmation_type")]
        public AuthGuardType ConfirmationType { get; set; }

        [JsonProperty("associated_message")]
        public string AssociatedMessage { get; set; }
    }
}
