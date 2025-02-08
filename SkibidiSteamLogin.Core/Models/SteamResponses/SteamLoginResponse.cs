using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Enums;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class SteamLoginResponse
    {
        [JsonProperty("client_id")]
        [JsonPropertyName("client_id")]
        internal string ClientId { get; set; }

        [JsonProperty("request_id")]
        [JsonPropertyName("request_id")]
        internal string RequestId { get; set; }

        [JsonProperty("interval")]
        [JsonPropertyName("interval")]
        internal int Interval { get; set; }

        [JsonProperty("allowed_confirmations")]
        [JsonPropertyName("allowed_confirmations")]
        internal List<AllowedConfirmation> AllowedConfirmations { get; set; }

        [JsonProperty("steamid")]
        [JsonPropertyName("steamid")]
        internal string SteamId { get; set; }

        [JsonProperty("weak_token")]
        [JsonPropertyName("weak_token")]
        internal string WeakToken { get; set; }

        [JsonProperty("extended_error_message")]
        [JsonPropertyName("extended_error_message")]
        internal string ExtendedErrorMessage { get; set; }
    }

    public class AllowedConfirmation
    {
        [JsonProperty("confirmation_type")]
        [JsonPropertyName("confirmation_type")]
        public AuthGuardTypeEnum ConfirmationType { get; set; }

        [JsonProperty("associated_message")]
        [JsonPropertyName("associated_message")]
        public string AssociatedMessage { get; set; }
    }
}
