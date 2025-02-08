using Newtonsoft.Json;

namespace SkibidiSteamLogin.Core.Models.SteamResponses
{
    internal class RsaData
    {
        public RsaData() { }

        public RsaData(string modulus, string exponent)
        {
            Modulus = modulus;
            Exponent = exponent;
        }

        [JsonProperty(PropertyName = "publickey_mod")]
        internal string Modulus { get; set; }

        [JsonProperty(PropertyName = "publickey_exp")]
        internal string Exponent { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        internal long Timestamp { get; set; }
    }
}
