using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Errors
{
    public record ErrorCodes
    {
        public string Code { get; }
        public int StatusCode { get; }

        private ErrorCodes(string code, int statusCode)
        {
            Code = code;
            StatusCode = statusCode;
        }

        // --- CÓDIGO MODIFICADO ---
        // Usamos una sintaxis alternativa para exponer los valores estáticos.

        public static ErrorCodes None => new("General.None", 200);
        public static ErrorCodes BadRequest => new("General.BadRequest", 400);
        public static ErrorCodes Unauthorized => new("Authentication.Unauthorized", 401);
        public static ErrorCodes NotFound => new("General.NotFound", 404);
        public static ErrorCodes Conflict => new("General.Conflict", 409);
        public static ErrorCodes Forbidden => new("Authorization.Forbidden", 403);
        public static ErrorCodes DatabaseError => new("Persistence.DatabaseError", 500);
        public static ErrorCodes Unexpected => new("General.Unexpected", 500);
        public static ErrorCodes TooManyRequests => new("General.TooManyRequests", 429);
    }


}
