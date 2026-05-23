using System.ComponentModel.DataAnnotations;

namespace Docquery.Server.Filters;

public sealed class DataAnnotationsValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return ValueTask.FromResult<object?>(Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Request body is required."]
            }));
        }

        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true))
        {
            return next(context);
        }

        return ValueTask.FromResult<object?>(Results.ValidationProblem(ToDictionary(validationResults)));
    }

    private static Dictionary<string, string[]> ToDictionary(IEnumerable<ValidationResult> validationResults)
    {
        return validationResults
            .SelectMany(
                result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, memberName) => new { memberName, result.ErrorMessage })
            .GroupBy(entry => entry.memberName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(entry => entry.ErrorMessage ?? "The request is invalid.")
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }
}