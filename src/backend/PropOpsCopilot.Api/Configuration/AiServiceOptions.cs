namespace PropOpsCopilot.Api.Configuration;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8000";
}
