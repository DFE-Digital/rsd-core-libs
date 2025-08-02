using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
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
        public DomainErrorCode? ErrorCode { get; }

        private Result(T value) { IsSuccess = true; Value = value; }

        private Result(DomainErrorCode code, string error)
        {
            IsSuccess = false;
            ErrorCode = code;
            Error = error;
        }

        public static Result<T> Success(T v) => new Result<T>(v);
        public static Result<T> Failure(string error) => new Result<T>(DomainErrorCode.BadRequest, error);
        public static Result<T> NotFound(string msg) => new Result<T>(DomainErrorCode.NotFound, msg);
        public static Result<T> Forbid(string msg) => new Result<T>(DomainErrorCode.Forbidden, msg);
        public static Result<T> Conflict(string msg) => new Result<T>(DomainErrorCode.Conflict, msg);
        public static Result<T> Validation(string msg) => new Result<T>(DomainErrorCode.Validation, msg);
    }
}