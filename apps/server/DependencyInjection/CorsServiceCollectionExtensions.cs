using Docquery.Server.Options;

namespace Docquery.Server.DependencyInjection;

public static class CorsServiceCollectionExtensions
{
    public const string FrontendCorsPolicyName = "FrontendCors";

    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<FrontendCorsOptions>()
            .BindConfiguration(FrontendCorsOptions.SectionName)
            .Validate(
                options => options.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins must contain at least one origin.")
            .Validate(
                options => options.AllowedOrigins.All(IsValidOrigin),
                "Each Cors:AllowedOrigins value must be an absolute origin without a path, query, or fragment.")
            .ValidateOnStart();

        var configuredOrigins = configuration
            .GetSection(FrontendCorsOptions.SectionName)
            .Get<FrontendCorsOptions>()?
            .AllowedOrigins
            .Select(static origin => origin.Trim().TrimEnd('/'))
            .ToArray()
            ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicyName, policy =>
            {
                policy
                    .WithOrigins(configuredOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static bool IsValidOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https"
            && !string.IsNullOrWhiteSpace(uri.Host)
            && (uri.AbsolutePath is "/" or "")
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }
}
