using System.ClientModel;
using Docquery.Server.Options;
using Docquery.Server.Services;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Docquery.Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentQa(this IServiceCollection services)
    {
        services
            .AddOptions<DocumentQaOptions>()
            .BindConfiguration(DocumentQaOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "DocumentQa:Model is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SystemPrompt), "DocumentQa:SystemPrompt is required.")
            .ValidateOnStart();

        services
            .AddOptions<OpenAiCompatibleOptions>()
            .BindConfiguration(OpenAiCompatibleOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _),
                "OpenAI:Endpoint must be an absolute URI.")
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
}