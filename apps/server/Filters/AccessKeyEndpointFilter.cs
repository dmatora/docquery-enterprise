using System.Security.Cryptography;
using System.Text;
using Docquery.Server.Options;
using Microsoft.Extensions.Options;

namespace Docquery.Server.Filters;

public sealed class AccessKeyEndpointFilter(IOptions<SecurityOptions> securityOptions) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Access-Key";

    private readonly byte[] _configuredAccessKeyBytes = Encoding.UTF8.GetBytes(securityOptions.Value.AccessKey);

    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        if (!headers.TryGetValue(HeaderName, out var headerValues))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        var providedAccessKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedAccessKey))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        var providedAccessKeyBytes = Encoding.UTF8.GetBytes(providedAccessKey);
        if (providedAccessKeyBytes.Length != _configuredAccessKeyBytes.Length
            || !CryptographicOperations.FixedTimeEquals(providedAccessKeyBytes, _configuredAccessKeyBytes))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        return next(context);
    }
}
