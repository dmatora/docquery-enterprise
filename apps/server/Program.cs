using System.ComponentModel.DataAnnotations;
using Docquery.Server.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapPost("/api/qa/ask", (DocumentAskRequest? request) =>
{
    if (request is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["body"] = ["Request body is required."]
        });
    }

    var validationErrors = Validate(request);
    if (validationErrors is not null)
    {
        return Results.ValidationProblem(validationErrors);
    }

    return Results.Problem(
        title: "Document QA is not implemented yet.",
        detail: "The request and response contract is ready. Wire MiniMax generation in the next step.",
        statusCode: StatusCodes.Status501NotImplemented);
})
    .WithName("AskDocumentQuestion")
    .WithSummary("Ask a question about the provided document text.")
    .WithDescription("Accepts the full document text and a user question, then returns the generated answer and processing metadata.")
    .Accepts<DocumentAskRequest>("application/json")
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces<DocumentAskResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status501NotImplemented)
    .WithOpenApi();

app.Run();

static Dictionary<string, string[]>? Validate(DocumentAskRequest request)
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();

    if (Validator.TryValidateObject(
        request,
        validationContext,
        validationResults,
        validateAllProperties: true))
    {
        return null;
    }

    return validationResults
        .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty),
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