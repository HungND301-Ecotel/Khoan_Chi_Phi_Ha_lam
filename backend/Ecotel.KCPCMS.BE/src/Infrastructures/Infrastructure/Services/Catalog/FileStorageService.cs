using Application.Dto.Catalog;
using Application.Interfaces.Services;


namespace Infrastructure.Services.Catalog;

public class FileStorageService(IHttpClientFactory httpClientFactory) : IFileStorageService
{
    public async Task<FileStreamResponse> GetFileStreamAsync(string presignedUrl)
    {
        var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.GetAsync(presignedUrl, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;

        var fileName =
            contentDisposition?.FileNameStar ??
            contentDisposition?.FileName ??
            "signature-file";

        fileName = fileName?.Trim('"');

        return new FileStreamResponse
        {
            Data = await response.Content.ReadAsStreamAsync(),
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
            FileName = fileName
        };
    }
}