using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Application.Dto.Cloud.AWS;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record ChangeEmployeeAvatarCommand(ChangeEmployeeAvatarDto UpdateModel) : IRequest<bool>;

public class ChangeEmployeeAvatarCommandHandler(IUnitOfWork unitOfWork, IAwsS3Service awsS3Service)
    : IRequestHandler<ChangeEmployeeAvatarCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<bool> Handle(ChangeEmployeeAvatarCommand request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EmployeeId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var oldAvatarPath = existEmployee.Avatar;

        existEmployee.SetAvatar(request.UpdateModel.AvatarUrl);
        _employeeRepository.Update(existEmployee);
        await unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldAvatarPath))
        {
            var bucketName = awsS3Service.GetBucketName(BucketType.SourceDefault);
            await awsS3Service.DeleteObject(bucketName, oldAvatarPath);
        }

        return true;
    }
}