namespace ChatApp.Client.Models.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string Error { get; }
        public int StatusCode { get; }

        private Result(bool isSuccess, T? value, string error, int statusCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result<T> Success(T value) => new(true, value, string.Empty, 200);
        public static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
    }

    // Non-generic version for operations that don't return data (like Delete)
    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public int StatusCode { get; }

        private Result(bool isSuccess, string error, int statusCode)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result Success() => new(true, string.Empty, 200);
        public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
    }
}