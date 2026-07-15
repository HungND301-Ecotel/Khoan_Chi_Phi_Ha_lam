using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Users.Queries;

public record class GetPermissionCatalogQuery() : IRequest<PermissionCatalogDto>;

public class GetPermissionCatalogQueryHandler(IUnitOfWork unitOfWork, IPermissionDefinitionScanner scanner) : IRequestHandler<GetPermissionCatalogQuery, PermissionCatalogDto>
{
    public async Task<PermissionCatalogDto> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var moduleRepo = unitOfWork.GetRepository<Module>();
        var permissionRepo = unitOfWork.GetRepository<Permission>();

        var modules = await moduleRepo.GetAllAsync(include: x => x.Include(m => m.SubModules), disableTracking: true);
        var permissions = await permissionRepo.GetAllAsync(disableTracking: true);

        var enforcedCodes = scanner.Scan()
            .Select(d => d.FullCode.ToLowerInvariant())
            .ToHashSet();

        var result = new PermissionCatalogDto
        {
            GlobalPermissions = permissions.OrderBy(p => p.Code).Select(p => new PermissionItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name
            }).ToList(),
            Modules = modules.OrderBy(m => m.SortOrder).Select(m => new ModuleCatalogDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                SortOrder = m.SortOrder,
                SubModules = m.SubModules.OrderBy(s => s.SortOrder).Select(s => new SubModuleCatalogDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    SortOrder = s.SortOrder,
                    AllowedPermissions = Enum.GetValues<PermissionCode>()
                        .Where(pc => enforcedCodes.Contains($"{m.Code}.{s.Code}.{pc}".ToLowerInvariant()))
                        .ToList()
                }).ToList()
            }).ToList()
        };

        return result;
    }
}