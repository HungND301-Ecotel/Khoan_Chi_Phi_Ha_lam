namespace Application.Utility;

public static class FileTypeHelper
{
    public static readonly HashSet<string> SupportedFiles =
    [
        "application/octet-stream",
        "image/jpeg",
        "image/png",
        "video/mp4",
        "video/quicktime",
        "video/x-msvideo"
    ];

    public static readonly HashSet<string> ImageMimeTypes =
    [
        "image/jpeg",
        "image/png"
    ];

    public static readonly HashSet<string> ImageExtensions =
    [
        ".jpeg",
        ".png"
    ];

    public static readonly HashSet<string> ImageTemplateMimeTypes = ["image/png"];

    public static readonly HashSet<string> VideoExtensions =
    [
        ".mp4",
        ".mov"
    ];

    public static readonly HashSet<string> VideoMimeTypes =
    [
        "video/mp4",
        "video/quicktime",
        "video/x-msvideo"
    ];

    public static readonly HashSet<string> LicenseExtensions = [".pdf", ".doc", ".docx"];

    public static bool IsImage(string mimeType)
    {
        return ImageMimeTypes.Contains(mimeType);
    }

    public static bool IsVideo(string fileName)
    {
        return VideoExtensions.Contains(Path.GetExtension(fileName));
    }

    public static bool IsSupported(string mimeType)
    {
        return SupportedFiles.Contains(mimeType);
    }
}