using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;

namespace Application.Catalog.Users.Queries;

public record class GetDepartmentPermissionsQuery(Guid DepartmentId) : IRequest<UpdateDepartmentPermissionsDto>;

public class GetDepartmentPermissionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetDepartmentPermissionsQuery, UpdateDepartmentPermissionsDto>
{
    public async Task<UpdateDepartmentPermissionsDto> Handle(GetDepartmentPermissionsQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<DepartmentModulePermission>();
        var perms = await repo.GetAllAsync(predicate: x => x.DepartmentId == request.DepartmentId, disableTracking: true);

        return new UpdateDepartmentPermissionsDto
        {
            DepartmentId = request.DepartmentId,
            Permissions = perms.Select(p => new DepartmentModulePermissionInputDto
            {
                ModuleId = p.ModuleId,
                PermissionId = p.PermissionId,
                IsGranted = p.IsGranted
            }).ToList()
        };
    }
}
