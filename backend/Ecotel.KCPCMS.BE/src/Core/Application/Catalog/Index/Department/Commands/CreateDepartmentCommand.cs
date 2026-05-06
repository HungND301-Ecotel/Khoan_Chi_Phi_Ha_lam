using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Department;
using Application.Interfaces.Services;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Department.Commands;

public record CreateDepartmentCommand(CreateDepartmentDto CreateModel) : IRequest<bool>;

public class CreateDepartmentCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateDepartmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Department>();

    public async Task<bool> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        var department = Domain.Entities.Index.Department.Create(request.CreateModel.Code, request.CreateModel.Name);
        await _departmentRepository.InsertAsync(department);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
