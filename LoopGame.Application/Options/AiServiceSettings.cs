namespace LoopGame.Application.Options;

public class AiServiceSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
}
