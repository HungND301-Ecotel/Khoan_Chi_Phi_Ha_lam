using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;

namespace Application.Catalog.Users.Queries;

public record class GetUserOverridePermissionsQuery(int UserId) : IRequest<UpdateUserOverridePermissionsDto>;

public class GetUserOverridePermissionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserOverridePermissionsQuery, UpdateUserOverridePermissionsDto>
{
    public async Task<UpdateUserOverridePermissionsDto> Handle(GetUserOverridePermissionsQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.GetRepository<UserPermissionOverride>();
        var overrides = await repo.GetAllAsync(predicate: x => x.UserId == request.UserId, disableTracking: true);

        return new UpdateUserOverridePermissionsDto
        {
            UserId = request.UserId,
            Overrides = overrides.Select(o => new UserPermissionOverrideInputDto
            {
                SubModuleId = o.SubModuleId,
                PermissionId = o.PermissionId,
                IsGranted = o.IsGranted,
                Reason = o.Reason
            }).ToList()
        };
    }
}
