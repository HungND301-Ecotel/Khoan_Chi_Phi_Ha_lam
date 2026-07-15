using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;

namespace Application.Catalog.Users.Commands;

public record class UpdatePositionPermissionsCommand(UpdatePositionPermissionsDto Dto) : IRequest<bool>;

public class UpdatePositionPermissionsCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCacheService cache)
    : IRequestHandler<UpdatePositionPermissionsCommand, bool>
{
    public async Task<bool> Handle(UpdatePositionPermissionsCommand request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<PositionSubmodulePermission>();
        var dto = request.Dto;

        // Xóa hết và insert lại (clean replace)
        var existingPerms = await repo.GetAllAsync(
            predicate: x => x.PositionId == dto.PositionId,
            disableTracking: false);

        if (existingPerms.Any())
        {
            repo.Delete(existingPerms);
        }

        foreach (var p in dto.Permissions)
        {
            var newPerm = PositionSubmodulePermission.Create(dto.PositionId, p.SubModuleId, p.PermissionId, p.IsGranted);
            await repo.InsertAsync(newPerm);
        }

        await unitOfWork.SaveChangesAsync();

        // Invalidate cache của tất cả user thuộc chức vụ này
        var employeeRepo = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
        var employees = await employeeRepo.GetAllAsync(
            predicate: e => e.PositionId == dto.PositionId,
            disableTracking: true);

        var userIdList = employees.Select(e => e.UserId).Where(id => id > 0).ToList();
        if (userIdList.Count > 0)
        {
            await cache.InvalidateUsersAsync(userIdList, cancellationToken);
        }

        return true;
    }
}