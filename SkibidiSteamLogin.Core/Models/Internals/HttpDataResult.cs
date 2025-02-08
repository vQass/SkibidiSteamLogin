namespace SkibidiSteamLogin.Core.Models.Internals
{
    internal class HttpDataResult<T> : HttpResult where T : class
    {
        public T Data { get; set; }
    }
}
