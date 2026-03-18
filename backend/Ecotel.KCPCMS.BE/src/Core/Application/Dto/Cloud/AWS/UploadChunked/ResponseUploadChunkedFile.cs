namespace Application.Dto.Cloud.AWS.UploadChunked;

public class ResponseUploadChunkedFile
{
    public string UploadId { get; set; } = string.Empty;
    public int PartNumber { get; set; }
    public string ETag { get; set; } = string.Empty;
}