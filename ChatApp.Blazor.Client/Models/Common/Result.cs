namespace ChatApp.Blazor.Client.Models.Common
{
    public record Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        protected Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(string error)=> new(false, error);
        public static Result<T> Success<T>(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure<T>(string error) => new Result<T>(false, default, error);
    }

    public record Result<T> : Result
    {
        public T? Value { get; }
        internal Result(bool isSuccess, T? value, string? error) : base(isSuccess, error)
        {
            Value= value;
        }
    }
}