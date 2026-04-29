


namespace Domain.Errors
{
    public record ErrorCodes
    {

        /// Indica que la solicitud del cliente es inválida, falta información, o hay inconsistencia de IDs (HTTP 400)
        public const string BadRequest = "BadRequest";

        /// Indica que un recurso ya existe y viola una restricción de unicidad (e.g., email, DNI ya en uso) (HTTP 409).
        public const string Conflict = "Conflict";

        /// Indica que el recurso solicitado o una dependencia no fue encontrada (HTTP 404).
        public const string NotFound = "NotFound";

        /// Indica que las credenciales son inválidas o que el usuario no está autorizado (HTTP 401).
        public const string Unauthorized = "Unauthorized";

        /// Indica que la cuenta ha sido bloqueada debido a demasiados intentos fallidos (HTTP 429).
        public const string TooManyRequests = "TooManyRequests";

        /// Indica una excepción de sistema o error de base de datos no controlado (HTTP 500).
        public const string Unexpected = "Unexpected";

    }


}
