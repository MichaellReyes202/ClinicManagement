using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Errors
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }
        public string ErrorCode { get; set; }
        public List<ValidationError> ValidationErrors { get; }

        protected Result(bool isSuccess, string error , string errorCode, List<ValidationError> validationErrors)
        {
            IsSuccess = isSuccess;
            Error = error;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors ?? new List<ValidationError>();
        }

        public static Result Success() => new Result(true,null ,null, null);
        public static Result Failure(string error,string errorcode) => new Result(false, error,errorcode, null);
        public static Result Failure(List<ValidationError> validationErrors) => new Result(false,null, null, validationErrors);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        protected Result(bool isSuccess, T value, string error , string errorCode , List<ValidationError> validationErrors)
            : base(isSuccess, error,errorCode, validationErrors)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value,null, null, null);
        public new static Result<T> Failure(string error , string errorCode) => new Result<T>(false, default(T), error,errorCode, null);
        public new static Result<T> Failure(List<ValidationError> validationErrors) => new Result<T>(false, default(T), null ,null, validationErrors);
    }
}
