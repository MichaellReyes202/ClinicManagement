namespace Application.Models;

public class AiSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string ApiKey { get; set; } = "ollama";
    public string ModelId { get; set; } = "qwen2.5:7b";
    public int ContextWindowSize { get; set; } = 5;
    public int HttpTimeoutSeconds { get; set; } = 180;
}
