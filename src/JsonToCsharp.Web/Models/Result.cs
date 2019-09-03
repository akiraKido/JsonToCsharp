namespace JsonToCsharp.Web.Models
{
    public class Result<T>
    {
        private Result(bool success, T value, string error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public bool Success { get; set; }
        public T Value { get; set; }
        public string Error { get; set; }

        public static Result<T> CreateSuccess(T value) => new Result<T>(true, value, null);
        public static Result<T> CreateFail(string error) => new Result<T>(false, default, error);
    }
}