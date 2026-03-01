using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class SteamResponseWrapper<T>
    {
        [JsonProperty("Response")]
        public T Data { get; set; }
    }
}
