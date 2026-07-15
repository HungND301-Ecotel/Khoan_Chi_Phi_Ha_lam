using Application.Common.Exceptions;
using Application.Dto.Cloud.AWS;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Index.Employee.Commands;

public record UploadEmployeeImageCommand(List<IFormFile> Files, string FolderPath) : IRequest<List<string>>;

public class UploadEmployeeImageCommandHandler(IAwsS3Service awsS3Service)
    : IRequestHandler<UploadEmployeeImageCommand, List<string>>
{

    public async Task<List<string>> Handle(UploadEmployeeImageCommand request, CancellationToken cancellationToken)
    {
        if (request.Files == null || !request.Files.Any())
        {
            throw new BadRequestException("Vui lòng chọn file ảnh.");
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var urls = new List<string>();

        foreach (var file in request.Files)
        {
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                throw new BadRequestException($"File '{file.FileName}' không hợp lệ. Chỉ chấp nhận JPG, PNG, WEBP.");
            }

            var input = new AwsInputModel
            {
                FileId = Guid.NewGuid(),
                FileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}",
                BucketType = BucketType.SourceDefault,
                Module = request.FolderPath,
                ContentType = file.ContentType,
                IsExpires = false
            };

            var result = await awsS3Service.UploadFileAsync(file, input);
            urls.Add(result.Path);
        }

        return urls;
    }
}