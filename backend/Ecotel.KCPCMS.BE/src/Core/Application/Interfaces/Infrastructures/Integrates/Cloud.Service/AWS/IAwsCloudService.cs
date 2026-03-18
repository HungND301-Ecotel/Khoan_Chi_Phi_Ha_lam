using Application.Dto.Cloud.AWS;
using Application.Dto.Cloud.AWS.UploadChunked;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;

public interface IAwsCloudService
{
    bool IsAwsUrlExpired(string path);

    Task<AwsUploadedOutputModel> UploadFileAsync(IFormFile fileData, AwsInputModel awsInput);

    Task CopyObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey);

    Task MoveObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey);

    Task DeleteObjectAsync(string bucket, string path);

    Task<ResponseUploadChunkedFile> UploadFileChunkedAsync(Stream fileStream, UploadChunkedRequest awsInput);

    Task<AwsUploadedOutputModel> CompleteChunkedUpload(CompleteUploadChunkedModel request);

    Task<InitiateUploadChunkedResponse> InitiateChunkedUpload(InitiateUploadChunkedRequest request);
}