using Microsoft.AspNetCore.Http;

namespace ServiceRequestService.Services;

public interface IDocumentStorageClient
{
    Task<Guid> UploadSupportingDocumentAsync(Guid serviceRequestId, IFormFile file, string? authorizationHeader, CancellationToken cancellationToken = default);
}
