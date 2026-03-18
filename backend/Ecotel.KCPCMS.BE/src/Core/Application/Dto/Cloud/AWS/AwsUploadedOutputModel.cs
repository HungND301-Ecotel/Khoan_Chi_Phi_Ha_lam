namespace Application.Dto.Cloud.AWS;

public class AwsUploadedOutputModel
{
    public string Path { get; set; } = string.Empty;
    public string? FullPath { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public decimal FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? FileUniqueName { get; set; }
    public string BucketName { get; set; } = string.Empty;
}