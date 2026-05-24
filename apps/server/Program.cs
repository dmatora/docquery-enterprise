using Docquery.Server.DependencyInjection;
using Docquery.Server.Endpoints;
using Docquery.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguredCors(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DocumentQaExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDocumentQa();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors(Docquery.Server.DependencyInjection.CorsServiceCollectionExtensions.FrontendCorsPolicyName);

app.MapQaEndpoints();

app.Run();
