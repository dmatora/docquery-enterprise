using System.ComponentModel.DataAnnotations;

namespace Docquery.Server.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Required(AllowEmptyStrings = false)]
    public required string AccessKey { get; init; }
}
