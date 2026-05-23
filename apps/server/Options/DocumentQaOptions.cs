using System.ComponentModel.DataAnnotations;

namespace Docquery.Server.Options;

public sealed class DocumentQaOptions
{
    public const string SectionName = "DocumentQa";

    [Required(AllowEmptyStrings = false)]
    public required string Model { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string SystemPrompt { get; init; }

    [Range(1, 600)]
    public int RequestTimeoutSeconds { get; init; } = 90;
}