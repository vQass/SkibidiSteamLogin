namespace SkibidiSteamLogin.Core.Models.Externals
{
    public class OperationResult<T> where T : class
    {
        public bool IsSuccess { get; private init; }
        public T Data { get; private init; }
        public string ErrorMessage { get; private init; }

        public static OperationResult<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        public static OperationResult<T> Failure(string errorMessage) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
