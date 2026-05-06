using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Department;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Department.Commands;

public record UpdateDepartmentCommand(UpdateDepartmentDto UpdateModel) : IRequest<bool>;

public class UpdateDepartmentCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateDepartmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Department>();

    public async Task<bool> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var existDepartment = await _departmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: q => q.Include(d => d.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existDepartment.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        existDepartment.Update(request.UpdateModel.Code, request.UpdateModel.Name);
        _departmentRepository.Update(existDepartment);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
