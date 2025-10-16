
using System.Text.Json.Serialization;


namespace Domain.Errors;


public class Error
{
    // 1. Cambiar el tipo de dato de Code a string
    // Almacena la cadena del código (ej: "Conflict", "NotFound")
    [JsonPropertyName("code")]
    public string Code { get; }

    public string Description { get; }

    // El campo puede ser null, por eso no lleva required
    public string? Field { get; }

    // Constructor para errores generales (sin campo específico)
    public Error(string code, string description)
    {
        Code = code;
        Description = description;
        Field = null; // Se deja nulo
    }

    // Constructor para errores de campo específico (e.g., 409 Conflict)
    public Error(string code, string description, string field)
    {
        Code = code;
        Description = description;
        Field = field;
    }
}


//public class Error
//{
//    [JsonIgnore]
//    public ErrorCodes Code { get; }

//    [JsonPropertyName("code")] // Este se serializa en su lugar
//    public string CodeValue => Code.Code;

//    public string Description { get; }
//    public string Field { get; }

//    //Constructor para errores generales(sin campo específico)
//    public Error(ErrorCodes code, string description)
//    {
//        Code = code;
//        Description = description;
//        Field = null; // Se deja vacío/nulo
//    }

//    // Nuevo Constructor para errores de campo específico (e.g., 409 Conflict)
//    public Error(ErrorCodes code, string description, string field)
//    {
//        Code = code;
//        Description = description;
//        Field = field;
//    }
//}
