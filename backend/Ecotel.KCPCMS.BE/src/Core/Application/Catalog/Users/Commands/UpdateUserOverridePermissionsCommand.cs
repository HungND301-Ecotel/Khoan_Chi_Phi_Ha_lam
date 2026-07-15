using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;

namespace Application.Catalog.Users.Commands;

public record class UpdateUserOverridePermissionsCommand(UpdateUserOverridePermissionsDto Dto) : IRequest<bool>;

public class UpdateUserOverridePermissionsCommandHandler(IUnitOfWork unitOfWork, IPermissionCacheService cache)
    : IRequestHandler<UpdateUserOverridePermissionsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserOverridePermissionsCommand request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<UserPermissionOverride>();
        var dto = request.Dto;

        var existingOverrides = await repo.GetAllAsync(
            predicate: x => x.UserId == dto.UserId,
            disableTracking: false);

        if (existingOverrides.Any())
        {
            repo.Delete(existingOverrides);
        }

        foreach (var o in dto.Overrides)
        {
            var newOverride = UserPermissionOverride.Create(dto.UserId, o.SubModuleId, o.PermissionId, o.IsGranted, o.Reason ?? string.Empty);
            await repo.InsertAsync(newOverride);
        }

        await unitOfWork.SaveChangesAsync();
        
        // Invalidate cache cho user này
        await cache.InvalidateUserAsync(dto.UserId, cancellationToken);
        
        return true;
    }
}