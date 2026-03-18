using Application.Configurations;
using Application.Dto.Cloud.AWS;

namespace Application.Utility.AWS;

public static class AwsS3Extension
{
    public static string GenerateAwsS3Key(string folder, string id)
    {
        return string.IsNullOrEmpty(folder) ? $"{id}" : $"{folder}/{id}";
    }

    public static string GenAwsFolder(string rootFolder, string placeFolder, string module)
    {
        return string.IsNullOrEmpty(rootFolder)
            ? $"{module}/{placeFolder}"
            : $"{rootFolder}/{module}/{placeFolder}";
    }

    public static string GenAwsFileName(string uniqueFileName, string extension = "", string prefix = "")
    {
        return string.IsNullOrEmpty(prefix)
            ? $"{uniqueFileName}{extension}"
            : $"{prefix}_{uniqueFileName}{extension}";
    }

    public static string? GetS3Url(AwsS3Configuration configuration, string? filePath, BucketType bucketType)
    {
        if (configuration == null)
        {
            throw new NullReferenceException("CONFIGURATION_NOT_LOADED");
        }

        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        if (filePath.StartsWith("http://", StringComparison.Ordinal) || filePath.StartsWith("https://", StringComparison.Ordinal))
        {
            return filePath;
        }

        return bucketType switch
        {
            BucketType.SourceDefault => configuration.Uri.SourceDefault + filePath,
            _ => filePath
        };
    }
}