using System.ClientModel;
using Docquery.Server.Options;
using Docquery.Server.Services;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Docquery.Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string ConfigureViaUserSecretsPlaceholder = "__CONFIGURE_VIA_USER_SECRETS__";

    public static IServiceCollection AddDocumentQa(this IServiceCollection services)
    {
        services
            .AddOptions<DocumentQaOptions>()
            .BindConfiguration(DocumentQaOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => IsConfigured(options.Model), "DocumentQa:Model must be configured via user secrets or environment variables.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SystemPrompt), "DocumentQa:SystemPrompt is required.")
            .ValidateOnStart();

        services
            .AddOptions<OpenAiCompatibleOptions>()
            .BindConfiguration(OpenAiCompatibleOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => IsConfigured(options.Endpoint),
                "OpenAI:Endpoint must be configured via user secrets or environment variables.")
            .Validate(
                options => !IsConfigured(options.Endpoint) || Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _),
                "OpenAI:Endpoint must be an absolute URI.")
            .Validate(
                options => IsConfigured(options.ApiKey),
                "OpenAI:ApiKey must be configured via user secrets or environment variables.")
            .ValidateOnStart();

        services
            .AddOptions<SecurityOptions>()
            .BindConfiguration(SecurityOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => IsConfigured(options.AccessKey),
                "Security:AccessKey must be configured via user secrets or environment variables.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
        {
            var providerOptions = serviceProvider.GetRequiredService<IOptions<OpenAiCompatibleOptions>>().Value;
            var documentQaOptions = serviceProvider.GetRequiredService<IOptions<DocumentQaOptions>>().Value;

            return new ChatClient(
                model: documentQaOptions.Model,
                credential: new ApiKeyCredential(providerOptions.ApiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(providerOptions.Endpoint)
                });
        });

        services.AddScoped<IDocumentQaService, ChatCompletionService>();

        return services;
    }

    private static bool IsConfigured(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value, ConfigureViaUserSecretsPlaceholder, StringComparison.Ordinal);
    }
}
