using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class Result<T>
    {
        [JsonPropertyName("value")]
        public T? Value { get; }
        
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; }
        
        [JsonPropertyName("error")]
        public string? Error { get; }

        private Result(T value, bool isSuccess, string? error)
        {
            Value = value;
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(value, true, null);
        public static Result<T> Failure(string error) => new Result<T>(default!, false, error);
    }
}
