using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog;
using Application.Dto.Cloud.AWS;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using Application.Interfaces.Services;
using Domain.Entities.Identity;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Queries;

public record GetMySignatureFileQuery(Guid SignatureId) : IRequest<FileStreamResponse>;

public class GetMySignatureFileQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAwsS3Service awsS3Service,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetMySignatureFileQuery, FileStreamResponse>
{
    private readonly IWriteRepository<UserSignature> _signatureRepository =
        unitOfWork.GetRepository<UserSignature>();

    private static readonly TimeSpan InternalFetchLifetime = TimeSpan.FromMinutes(5);

    public async Task<FileStreamResponse> Handle(GetMySignatureFileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var signature = await _signatureRepository.GetFirstOrDefaultAsync(
            predicate: s => s.Id == request.SignatureId
                            && s.UserId == userId
                            && s.IsActive,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (string.IsNullOrEmpty(signature.SignatureFile))
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var presignedUrl = await awsS3Service.GeneratePresignedUrlAsync(
            signature.SignatureFile,
            BucketType.SourceDefault,
            InternalFetchLifetime);

        return await fileStorageService.GetFileStreamAsync(presignedUrl);
    }
}