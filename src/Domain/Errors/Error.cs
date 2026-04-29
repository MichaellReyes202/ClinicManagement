

using System.Text.Json.Serialization;

namespace Domain.Errors;

public class Error
{
  [JsonPropertyName("code")]
  public string Code { get; }

  [JsonPropertyName("description")]
  public string Description { get; }

  [JsonPropertyName("field")]
  public string? Field { get; }

  [JsonPropertyName("metadata")]
  public Dictionary<string, object>? Metadata { get; }

  [JsonPropertyName("validationErrors")]
  public List<ValidationError>? ValidationErrors { get; }

  public Error(string code, string description, string? field = null, Dictionary<string, object>? metadata = null, List<ValidationError>? validationErrors = null)
  {
    Code = code;
    Description = description;
    Field = field;
    Metadata = metadata;
    ValidationErrors = validationErrors;
  }
}






//using System.Text.Json.Serialization;


//namespace Domain.Errors;

//public class Error
//{
//    [JsonPropertyName("code")]
//    public string Code { get; }

//    [JsonPropertyName("description")]
//    public string Description { get; }

//    [JsonPropertyName("field")]
//    public string? Field { get; }

//    [JsonPropertyName("metadata")]
//    public Dictionary<string, object>? Metadata { get; }


//    public Error(string code, string description, string? field = null, Dictionary<string, object>? metadata = null)
//    {
//        Code = code;
//        Description = description;
//        Field = field;
//        Metadata = metadata;
//    }
//}

