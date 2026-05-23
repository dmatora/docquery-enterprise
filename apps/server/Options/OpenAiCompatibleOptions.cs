using System.ComponentModel.DataAnnotations;

namespace Docquery.Server.Options;

public sealed class OpenAiCompatibleOptions
{
    public const string SectionName = "OpenAI";

    [Required(AllowEmptyStrings = false)]
    public required string Endpoint { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string ApiKey { get; init; }
}