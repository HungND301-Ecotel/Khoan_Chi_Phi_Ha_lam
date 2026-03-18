using Microsoft.Extensions.Configuration;

namespace Application.Helpers;

public static class FileStorageHelper
{
    public static string GetFileStorageFullPath(string path, IConfiguration configuration)
    {
        if (path.StartsWith("http://", StringComparison.Ordinal) || path.StartsWith("https://", StringComparison.Ordinal))
        {
            return path;
        }

        string baseUri = configuration["FileStorageSettings:Uri"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUri))
        {
            throw new InvalidOperationException("FileStorageSettings:Uri is not configured.");
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            return baseUri;
        }
        return baseUri + path;
    }
}