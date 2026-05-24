namespace Docquery.Server.Options;

public sealed class FrontendCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
