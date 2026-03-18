using Amazon.S3;
using Application.Dto.Cloud.AWS;
using Application.Dto.Cloud.AWS.UploadChunked;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Integrates;

public class AwsCloudService(
    IAwsS3Service awsS3Service)
    : IAwsCloudService
{
    public async Task<AwsUploadedOutputModel> UploadFileAsync(IFormFile fileData, AwsInputModel awsInput)
    {
        if (string.IsNullOrEmpty(awsInput.FileName) || awsInput.FileId == Guid.Empty)
        {
            throw new Exception("File name or file id is invalid.");
        }

        awsInput.CannedAcl = S3CannedACL.PublicRead;

        return await awsS3Service.UploadFileAsync(fileData, awsInput);
    }

    public bool IsAwsUrlExpired(string path)
    {
        return awsS3Service.IsAwsUrlExpired(path);
    }

    public Task CopyObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey)
    {
        return awsS3Service.CopyObjectAsync(srcBucket, srcKey, destBucket, destKey);
    }

    public Task MoveObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey)
    {
        return awsS3Service.MoveObjectAsync(srcBucket, srcKey, destBucket, destKey);
    }

    public Task DeleteObjectAsync(string bucket, string path)
    {
        return awsS3Service.DeleteObject(bucket, path);
    }

    public Task<ResponseUploadChunkedFile> UploadFileChunkedAsync(Stream fileStream, UploadChunkedRequest awsInput)
    {
        return awsS3Service.UploadFileChunkedAsync(fileStream, awsInput);
    }

    public Task<AwsUploadedOutputModel> CompleteChunkedUpload(CompleteUploadChunkedModel request)
    {
        return awsS3Service.CompleteChunkedUpload(request);
    }

    public Task<InitiateUploadChunkedResponse> InitiateChunkedUpload(InitiateUploadChunkedRequest request)
    {
        return awsS3Service.InitiateChunkedUpload(request);
    }
}