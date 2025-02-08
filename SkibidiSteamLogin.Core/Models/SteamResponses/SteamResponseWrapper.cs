using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class SteamResponseWrapper<T>
    {
        [JsonProperty("Response")]
        [JsonPropertyName("Response")]
        public T Data { get; set; }
    }
}
