using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ServiceRequestService.Services;

public class DocumentStorageClient : IDocumentStorageClient
{
    private readonly HttpClient _httpClient;

    public DocumentStorageClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> UploadSupportingDocumentAsync(Guid serviceRequestId, IFormFile file, string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        using var multipart = new MultipartFormDataContent();

        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/pdf"
            : file.ContentType);

        multipart.Add(new StringContent(serviceRequestId.ToString()), "ServiceRequestId");
        multipart.Add(fileContent, "File", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/documents/supporting-files")
        {
            Content = multipart
        };

        if (!string.IsNullOrWhiteSpace(authorizationHeader))
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new ArgumentException($"Document upload failed: {body}");

        using var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("id", out var idElement) || !idElement.TryGetGuid(out var documentId))
            throw new InvalidOperationException("Document service upload response did not include a valid document id.");

        return documentId;
    }
}
