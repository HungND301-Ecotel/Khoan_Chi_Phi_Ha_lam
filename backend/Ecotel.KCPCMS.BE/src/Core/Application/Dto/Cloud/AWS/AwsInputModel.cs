using Amazon.S3;
using Amazon.S3.Model;

namespace Application.Dto.Cloud.AWS;

public class AwsBaseInput
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public S3CannedACL? CannedAcl { get; set; }
    public BucketType BucketType { get; set; }
}

public class AwsInputModel : AwsBaseInput
{
    public List<Tag> TagSet { get; set; } = new();
    public bool IsExpires { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}

public enum BucketType
{
    SourceDefault = 1,
}