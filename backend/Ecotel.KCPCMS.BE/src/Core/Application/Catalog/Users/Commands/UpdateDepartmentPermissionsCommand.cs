using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Users.Commands;

public record class UpdateDepartmentPermissionsCommand(UpdateDepartmentPermissionsDto Dto) : IRequest<bool>;

public class UpdateDepartmentPermissionsCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCacheService cache)
    : IRequestHandler<UpdateDepartmentPermissionsCommand, bool>
{
    public async Task<bool> Handle(UpdateDepartmentPermissionsCommand request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<DepartmentModulePermission>();
        var dto = request.Dto;

        // Xóa hết và insert lại (clean replace)
        var existingPerms = await repo.GetAllAsync(
            predicate: x => x.DepartmentId == dto.DepartmentId,
            disableTracking: false);

        if (existingPerms.Any())
        {
            repo.Delete(existingPerms);
        }

        foreach (var p in dto.Permissions)
        {
            var newPerm = DepartmentModulePermission.Create(dto.DepartmentId, p.ModuleId, p.PermissionId, p.IsGranted);
            await repo.InsertAsync(newPerm);
        }

        await unitOfWork.SaveChangesAsync();

        // Invalidate cache của tất cả user thuộc phòng ban này
        var employeeRepo = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
        var userIds = await employeeRepo.GetAllAsync(
            predicate: e => e.DepartmentId == dto.DepartmentId,
            disableTracking: true);

        var userIdList = userIds.Select(e => e.UserId).Where(id => id > 0).ToList();
        if (userIdList.Count > 0)
        {
            await cache.InvalidateUsersAsync(userIdList, cancellationToken);
        }

        return true;
    }
}