using Amazon.S3.Model;
using Application.Dto.Cloud.AWS;
using Application.Dto.Cloud.AWS.UploadChunked;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;

public interface IAwsS3Service
{
    Task<AwsUploadedOutputModel> UploadFileAsync(IFormFile fileData, AwsInputModel input);

    Task<AwsUploadedOutputModel> UploadFileAsync(byte[] fileData, AwsInputModel input);

    Task<string> GeneratePresignedUrlAsync(string key, BucketType bucketType, TimeSpan expiry);

    Task CopyObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey);

    Task MoveObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey);

    bool IsAwsUrlExpired(string path);

    Task DeleteObject(string bucket, string path);

    Task<DeleteObjectResponse> DeleteObjectByUrl(string url);

    string GetBucketName(BucketType bucketType);

    Task<InitiateUploadChunkedResponse> InitiateChunkedUpload(InitiateUploadChunkedRequest request);

    Task<ResponseUploadChunkedFile> UploadFileChunkedAsync(Stream fileStream, UploadChunkedRequest input);

    Task<AwsUploadedOutputModel> CompleteChunkedUpload(CompleteUploadChunkedModel request);
}