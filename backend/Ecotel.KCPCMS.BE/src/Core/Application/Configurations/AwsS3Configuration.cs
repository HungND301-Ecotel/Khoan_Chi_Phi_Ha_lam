namespace Application.Configurations;

public class AwsS3Configuration
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string RootFolder { get; set; } = string.Empty;
    public string DefaultPlaceFolder { get; set; } = string.Empty;
    public int MaxSizeUploadMb { get; set; }
    public int MaxSizeUploadByte => MaxSizeUploadMb * 1024 * 1024;
    public bool ResizeImage { get; set; }
    public int MinImageWidth { get; set; }
    public int MinImageHeight { get; set; }
    public IList<ResizeOptionConfiguration> ResizeOptions { get; set; } = new List<ResizeOptionConfiguration>();
    public Uri Uri { get; set; } = new();
    public Bucket Bucket { get; set; } = new();
}

public class Uri
{
    public string SourceDefault { get; set; } = string.Empty;
}

public class Bucket
{
    public string SourceDefault { get; set; } = string.Empty;
}

public class ResizeOptionConfiguration
{
    public double DivideUnit { get; set; }
    public int Quality { get; set; }
    public string FolderName { get; set; } = string.Empty;
}