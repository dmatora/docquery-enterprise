using Docquery.Server.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/qa/ask", (DocumentAskRequest _) =>
    Results.Problem(
        title: "Document QA is not implemented yet.",
        detail: "The request and response contract is ready. Wire MiniMax generation in the next step.",
        statusCode: StatusCodes.Status501NotImplemented))
    .WithName("AskDocumentQuestion")
    .Accepts<DocumentAskRequest>("application/json")
    .Produces<DocumentAskResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status501NotImplemented)
    .WithOpenApi();

app.Run();