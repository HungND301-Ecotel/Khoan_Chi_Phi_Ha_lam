using System.Globalization;
using System.Web;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Configurations;
using Application.Dto.Cloud.AWS;
using Application.Dto.Cloud.AWS.UploadChunked;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using Application.Utility.AWS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BucketType = Application.Dto.Cloud.AWS.BucketType;
using Uri = System.Uri;

namespace External.Service.AWS;

public class AwsS3Service : IAwsS3Service
{
    private const string DateFormat = "yyyyMMddTHHmmssZ";
    private readonly AwsS3Configuration _awsS3Configuration;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<AwsS3Service> _logger;
    private readonly ICurrentUser _currentUser;

    public AwsS3Service(
        IOptions<AwsS3Configuration> awsS3Configuration,
        ILogger<AwsS3Service> logger,
        ICurrentUser currentUser
        )
    {
        _awsS3Configuration = awsS3Configuration.Value;
        _logger = logger;
        _currentUser = currentUser;

        var credentials = new BasicAWSCredentials(_awsS3Configuration.AccessKeyId, _awsS3Configuration.SecretAccessKey);
        var fileRegion = RegionEndpoint.GetBySystemName(_awsS3Configuration.Region);

        _s3Client = new AmazonS3Client(credentials, fileRegion);
    }

    public async Task<AwsUploadedOutputModel> UploadFileAsync(IFormFile fileData, AwsInputModel input)
    {
        if (fileData.Length > _awsS3Configuration.MaxSizeUploadByte)
        {
            throw new BadRequestException("EXCEEDED_FILE_LIMIT");
        }

        var fileTransferUtility = new TransferUtility(_s3Client);

        string extension = Path.GetExtension(input.FileName);

        input.FileId = Guid.NewGuid();

        var request = CreateRequest(fileData.OpenReadStream(), input);

        // AWS file upload origin file
        await fileTransferUtility.UploadAsync(request);

        string path = AwsS3Extension.GenerateAwsS3Key(
            AwsS3Extension.GenAwsFolder(_awsS3Configuration.RootFolder, _awsS3Configuration.DefaultPlaceFolder, input.Module),
            AwsS3Extension.GenAwsFileName(input.FileId.ToString(), extension));

        var response = new AwsUploadedOutputModel
        {
            Path = path,
            FullPath = AwsS3Extension.GetS3Url(_awsS3Configuration, path, input.BucketType),
            Extension = extension,
            FileName = input.FileName,
            FileSize = fileData.Length,
            ContentType = fileData.ContentType,
            FileUniqueName = AwsS3Extension.GenAwsFileName(input.FileId.ToString(), extension),
            BucketName = GetBucketName(input.BucketType)
        };

        _logger.LogInformation("Uploaded");
        return response;
    }

    public async Task<AwsUploadedOutputModel> UploadFileAsync(byte[] fileData, AwsInputModel input)
    {
        if (fileData.Length > _awsS3Configuration.MaxSizeUploadByte)
        {
            throw new Exception("EXCEEDED_FILE_LIMIT");
        }

        var fileTransferUtility = new TransferUtility(_s3Client);

        string extension = Path.GetExtension(input.FileName);

        input.FileId = Guid.NewGuid();

        var stream = new MemoryStream(fileData);

        var request = CreateRequest(stream, input, extension);

        // AWS file upload origin file
        await fileTransferUtility.UploadAsync(request);

        string path = AwsS3Extension.GenerateAwsS3Key(
            AwsS3Extension.GenAwsFolder(_awsS3Configuration.RootFolder, _awsS3Configuration.DefaultPlaceFolder, input.Module),
            AwsS3Extension.GenAwsFileName(input.FileId.ToString(), extension));

        var response = new AwsUploadedOutputModel
        {
            Path = path,
            FullPath = AwsS3Extension.GetS3Url(_awsS3Configuration, path, input.BucketType),
            Extension = extension,
            FileName = input.FileName,
            FileSize = fileData.Length,
            ContentType = input.ContentType,
            FileUniqueName = AwsS3Extension.GenAwsFileName(input.FileId.ToString(), extension)
        };

        return response;
    }

    #region Chunked Upload

    public async Task<ResponseUploadChunkedFile> UploadFileChunkedAsync(Stream fileStream, UploadChunkedRequest input)
    {

        var uploadRequest = new UploadPartRequest
        {
            BucketName = GetBucketName(input.BucketType),
            Key = input.Key,
            UploadId = input.UploadId,
            PartNumber = input.PartNumber,
            InputStream = fileStream,
            PartSize = fileStream.Length
        };

        // AWS file upload with chunking
        var response = await _s3Client.UploadPartAsync(uploadRequest);

        return new ResponseUploadChunkedFile
        {
            UploadId = input.UploadId,
            PartNumber = response.PartNumber,
            ETag = response.ETag
        };
    }

    public async Task<InitiateUploadChunkedResponse> InitiateChunkedUpload(InitiateUploadChunkedRequest input)
    {
        string extension = Path.GetExtension(input.FileName);
        var fileId = Guid.NewGuid();
        string key = AwsS3Extension.GenerateAwsS3Key(
            AwsS3Extension.GenAwsFolder(_awsS3Configuration.RootFolder, _awsS3Configuration.DefaultPlaceFolder, input.Module),
            AwsS3Extension.GenAwsFileName(fileId.ToString(), extension));

        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = GetBucketName(input.BucketType),
            Key = key,
            Headers =
            {
                ContentDisposition = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(input.FileName)}{extension}"
            }
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

        return new InitiateUploadChunkedResponse
        {
            UploadId = response.UploadId,
            Key = response.Key,
            Module = input.Module,
            FileId = fileId
        };
    }

    public async Task<AwsUploadedOutputModel> CompleteChunkedUpload(CompleteUploadChunkedModel request)
    {
        string extension = Path.GetExtension(request.FileName);

        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = GetBucketName(request.BucketType),
            Key = request.Key,
            UploadId = request.UploadId,
            PartETags = request.PartETags.Select(e => new PartETag(e.PartNumber, e.ETag)).ToList()
        };

        await _s3Client.CompleteMultipartUploadAsync(completeRequest);

        // Get the object's metadata to retrieve the file size
        var metadataRequest = new GetObjectMetadataRequest
        {
            BucketName = GetBucketName(request.BucketType),
            Key = request.Key
        };

        var metadataResponse = await _s3Client.GetObjectMetadataAsync(metadataRequest);

        return new AwsUploadedOutputModel
        {
            Path = request.Key,
            FullPath = AwsS3Extension.GetS3Url(_awsS3Configuration, request.Key, request.BucketType),
            Extension = extension,
            FileName = request.FileName,
            FileSize = metadataResponse.ContentLength,
            ContentType = metadataResponse.Headers.ContentType,
            FileUniqueName = AwsS3Extension.GenAwsFileName(request.FileId.ToString(), extension)
        };
    }

    #endregion Chunked Upload

    public bool IsAwsUrlExpired(string path)
    {
        string uri = new Uri(path).Query;
        var queryParams = HttpUtility.ParseQueryString(uri);
        if (string.IsNullOrEmpty(queryParams["X-Amz-Expires"]) ||
            string.IsNullOrEmpty(queryParams["X-Amz-Date"]))
        {
            return false;
        }

        var expireDate = DateTime
            .ParseExact(queryParams["X-Amz-Date"]!, DateFormat, CultureInfo.InvariantCulture)
            .AddSeconds(int.Parse(queryParams["X-Amz-Expires"]!));

        return expireDate.CompareTo(DateTime.Now.AddDays(1)) < 0;
    }

    public async Task CopyObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = srcBucket,
            SourceKey = srcKey,
            DestinationBucket = destBucket,
            DestinationKey = destKey,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.CopyObjectAsync(request);
    }

    public async Task MoveObjectAsync(string srcBucket, string srcKey, string destBucket, string destKey)
    {
        await CopyObjectAsync(srcBucket, srcKey, destBucket, destKey);

        var requestRemove = new DeleteObjectRequest
        {
            BucketName = srcBucket,
            Key = srcKey
        };
        await _s3Client.DeleteObjectAsync(requestRemove);
    }

    public async Task DeleteObject(string bucket, string path)
    {
        var requestRemove = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = path
        };
        await _s3Client.DeleteObjectAsync(requestRemove);
    }

    public async Task<DeleteObjectResponse> DeleteObjectByUrl(string url)
    {
        var awsUri = new AmazonS3Uri(url);

        var requestRemove = new DeleteObjectRequest
        {
            BucketName = awsUri.Bucket,
            Key = awsUri.Key
        };

        return await _s3Client.DeleteObjectAsync(requestRemove);
    }

    public string GetBucketName(BucketType bucketType)
    {
        return bucketType switch
        {
            BucketType.SourceDefault => _awsS3Configuration.Bucket.SourceDefault,
            _ => string.Empty
        };
    }

    private TransferUtilityUploadRequest CreateRequest(Stream inputStream, AwsInputModel input)
    {
        return CreateRequest(inputStream, input, _awsS3Configuration.DefaultPlaceFolder);
    }

    private TransferUtilityUploadRequest CreateRequest(Stream inputStream, AwsInputModel input, string placeFolder)
    {
        string extension = Path.GetExtension(input.FileName);
        return new TransferUtilityUploadRequest
        {
            BucketName = GetBucketName(input.BucketType),
            Key = AwsS3Extension.GenerateAwsS3Key(
                AwsS3Extension.GenAwsFolder(_awsS3Configuration.RootFolder, placeFolder, input.Module),
                AwsS3Extension.GenAwsFileName(input.FileId.ToString(), extension)),
            CannedACL = input.CannedAcl ?? S3CannedACL.PublicRead,
            InputStream = inputStream,
            Headers =
            {
                ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(input.FileName)}{extension}"
            },
            ContentType = input.ContentType
        };
    }
}