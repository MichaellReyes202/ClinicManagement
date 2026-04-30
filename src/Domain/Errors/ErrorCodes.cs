

namespace Domain.Errors
{
  public static class ErrorCodes
  {
    // 400 - Bad Request
    public const string BadRequest = "BadRequest";
    public const string ValidationError = "ValidationError";
    public const string MissingRequiredField = "MissingRequiredField";
    public const string InvalidFormat = "InvalidFormat";

    // 401 - Unauthorized
    public const string Unauthorized = "Unauthorized";
    public const string InvalidCredentials = "InvalidCredentials";
    public const string TokenExpired = "TokenExpired";
    public const string InvalidToken = "InvalidToken";

    // 403 - Forbidden
    public const string Forbidden = "Forbidden";
    public const string InsufficientPermissions = "InsufficientPermissions";

    // 404 - Not Found
    public const string NotFound = "NotFound";
    public const string ResourceNotFound = "ResourceNotFound";

    // 405 - Method Not Allowed
    public const string MethodNotAllowed = "MethodNotAllowed";

    // 409 - Conflict
    public const string Conflict = "Conflict";
    public const string DuplicateResource = "DuplicateResource";
    public const string ResourceAlreadyExists = "ResourceAlreadyExists";

    // 422 - Unprocessable Entity
    public const string UnprocessableEntity = "UnprocessableEntity";
    public const string BusinessRuleViolation = "BusinessRuleViolation";

    // 429 - Too Many Requests
    public const string TooManyRequests = "TooManyRequests";
    public const string RateLimitExceeded = "RateLimitExceeded";

    // 500 - Internal Server Error
    public const string Unexpected = "Unexpected";
    public const string InternalServerError = "InternalServerError";
    public const string DatabaseError = "DatabaseError";
    public const string ExternalServiceError = "ExternalServiceError";

    // 503 - Service Unavailable
    public const string ServiceUnavailable = "ServiceUnavailable";
  }
}