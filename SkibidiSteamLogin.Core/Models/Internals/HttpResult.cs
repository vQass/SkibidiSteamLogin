using System.Net;

namespace SkibidiSteamLogin.Core.Models.Internals
{
    internal class HttpResult
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
