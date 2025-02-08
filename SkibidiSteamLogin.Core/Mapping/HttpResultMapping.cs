using SkibidiSteamLogin.Core.Models.Internals;

namespace SkibidiSteamLogin.Core.Mapping
{
    internal static class HttpResultMapping
    {
        internal static HttpResult ToHttpResult(this HttpResponseMessage result) 
        {
            return new HttpResult
            {
                StatusCode = result.StatusCode,
                IsSuccess = result.IsSuccessStatusCode,
            };
        }

        internal static HttpDataResult<T> ToHttpDataResult<T>(this HttpResponseMessage result, T data = null) where T : class
        {
            return new HttpDataResult<T>
            {
                StatusCode = result.StatusCode,
                IsSuccess = result.IsSuccessStatusCode,
                Data = data
            };
        }
    }
}
