using Docquery.Server.Contracts;

namespace Docquery.Server.Services;

public interface IDocumentQaService
{
    Task<DocumentAskResponse> AskAsync(DocumentAskRequest request, CancellationToken cancellationToken);
}