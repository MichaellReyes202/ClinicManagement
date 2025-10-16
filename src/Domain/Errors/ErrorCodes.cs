


namespace Domain.Errors
{
    public record ErrorCodes
    {

        /// <summary>
        /// Indica que la solicitud del cliente es inválida, falta información, o hay inconsistencia de IDs (HTTP 400).
        /// </summary>
        public const string BadRequest = "BadRequest";

        /// <summary>
        /// Indica que un recurso ya existe y viola una restricción de unicidad (e.g., email, DNI ya en uso) (HTTP 409).
        /// </summary>
        public const string Conflict = "Conflict";

        /// <summary>
        /// Indica que el recurso solicitado o una dependencia no fue encontrada (HTTP 404).
        /// </summary>
        public const string NotFound = "NotFound";

        /// <summary>
        /// Indica que las credenciales son inválidas o que el usuario no está autorizado (HTTP 401).
        /// </summary>
        public const string Unauthorized = "Unauthorized";

        /// <summary>
        /// Indica que la cuenta ha sido bloqueada debido a demasiados intentos fallidos (HTTP 429).
        /// </summary>
        public const string TooManyRequests = "TooManyRequests";

        /// <summary>
        /// Indica una excepción de sistema o error de base de datos no controlado (HTTP 500).
        /// </summary>
        public const string Unexpected = "Unexpected";

        // Puedes añadir más códigos de error de dominio aquí si los necesitas
        //public string Code { get; }
        //public int StatusCode { get; }

        //private ErrorCodes(string code, int statusCode)
        //{
        //    Code = code;
        //    StatusCode = statusCode;
        //}

        //// --- CÓDIGO MODIFICADO ---
        //// Usamos una sintaxis alternativa para exponer los valores estáticos.

        //public static ErrorCodes None => new("General.None", 200);
        //public static ErrorCodes BadRequest => new("General.BadRequest", 400);
        //public static ErrorCodes Unauthorized => new("Authentication.Unauthorized", 401);
        //public static ErrorCodes NotFound => new("General.NotFound", 404);
        //public static ErrorCodes Conflict => new("General.Conflict", 409);
        //public static ErrorCodes Forbidden => new("Authorization.Forbidden", 403);
        //public static ErrorCodes DatabaseError => new("Persistence.DatabaseError", 500);
        //public static ErrorCodes Unexpected => new("General.Unexpected", 500);
        //public static ErrorCodes TooManyRequests => new("General.TooManyRequests", 429);
    }


}
