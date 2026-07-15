using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;

namespace Application.Catalog.Users.Queries;

public record class GetPositionPermissionsQuery(int PositionId) : IRequest<UpdatePositionPermissionsDto>;

public class GetPositionPermissionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPositionPermissionsQuery, UpdatePositionPermissionsDto>
{
    public async Task<UpdatePositionPermissionsDto> Handle(GetPositionPermissionsQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<PositionSubmodulePermission>();
        var perms = await repo.GetAllAsync(predicate: x => x.PositionId == request.PositionId, disableTracking: true);

        return new UpdatePositionPermissionsDto
        {
            PositionId = request.PositionId,
            Permissions = perms.Select(p => new PositionSubmodulePermissionInputDto
            {
                SubModuleId = p.SubModuleId,
                PermissionId = p.PermissionId,
                IsGranted = p.IsGranted
            }).ToList()
        };
    }
}
