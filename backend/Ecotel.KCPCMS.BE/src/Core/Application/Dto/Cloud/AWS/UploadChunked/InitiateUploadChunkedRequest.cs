using System.Text.Json.Serialization;

namespace Application.Dto.Cloud.AWS.UploadChunked;

public class InitiateUploadChunkedRequest
{
    public string FileName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;

    [JsonIgnore]
    public BucketType BucketType { get; set; } = BucketType.SourceDefault;
}

public class UploadChunkedRequest
{
    public string UploadId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; 

    [JsonIgnore]
    public BucketType BucketType { get; set; } = BucketType.SourceDefault;

    public int PartNumber { get; set; }
}

public class InitiateUploadChunkedResponse
{
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public string UploadId { get; set; } = string.Empty;
}

public class CompleteUploadChunkedModel
{
    public string FileName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string UploadId { get; set; } = string.Empty;

    [JsonIgnore]
    public BucketType BucketType { get; set; } = BucketType.SourceDefault;

    public PartETagRequest[] PartETags { get; set; } = Array.Empty<PartETagRequest>();
}

public class PartETagRequest
{
    public int PartNumber { get; set; }
    public string ETag { get; set; } = string.Empty;
}