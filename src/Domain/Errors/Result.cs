


namespace Domain.Errors
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        // Ya no necesitamos ValidationErrors aquí, Error lo tiene todo
        public Error? Error { get; }

        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(Error error) => new(false, error);

        // Si se mandan una lista de validaciones, creamos el Error estructurado
        public static Result Failure(List<ValidationError> validationErrors) =>
            new(false, new Error(ErrorCodes.BadRequest, "Validation Problem", null, null, validationErrors));
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        protected Result(bool isSuccess, T? value, Error? error)
            : base(isSuccess, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public new static Result<T> Failure(Error error) => new(false, default, error);

        // Helper para genéricos
        public new static Result<T> Failure(List<ValidationError> validationErrors) =>
            new(false, default, new Error(ErrorCodes.BadRequest, "Validation Problem", null, null, validationErrors));
    }
}














//namespace Domain.Errors
//{
//    public class Result
//    {
//        public bool IsSuccess { get; }
//        public bool IsFailure => !IsSuccess;
//        public List<ValidationError> ValidationErrors { get; }
//        public Error? Error { get; }

//        // Constructor para un resultado fallido
//        protected Result(bool isSuccess, Error? error, List<ValidationError>? validationErrors)
//        {
//            IsSuccess = isSuccess;
//            Error = error;
//            ValidationErrors = validationErrors ?? new List<ValidationError>();
//        }

//        public static Result Success() => new(true, null, null);
//        public static Result Failure(Error error) => new(false, error, null);
//        public static Result Failure(List<ValidationError> validationErrors) => new(false, null, validationErrors);
//    }

//    public class Result<T> : Result
//    {
//        public T? Value { get; }

//        protected Result(bool isSuccess, T? value, Error? error, List<ValidationError>? validationErrors)
//            : base(isSuccess, error, validationErrors)
//        {
//            Value = value;
//        }

//        public static Result<T> Success(T value) => new(true, value, null, null);
//        public new static Result<T> Failure(Error error) => new(false, default, error, null);
//        public new static Result<T> Failure(List<ValidationError> validationErrors) =>
//            new(false, default, new Error(ErrorCodes.BadRequest, "Validation Problem"), validationErrors);
//    }
//}
