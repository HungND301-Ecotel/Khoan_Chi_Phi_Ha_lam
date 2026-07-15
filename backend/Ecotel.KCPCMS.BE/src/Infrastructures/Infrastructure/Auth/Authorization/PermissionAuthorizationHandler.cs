using Application.Common.Interfaces;
using Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth.Authorization;

internal class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    IPermissionCacheService cache)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(nameof(RoleType.SystemAdmin)))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = currentUser.GetUserId();

        if (userId <= 0)
        {
            context.Fail();
            return;
        }

        var permissions = await cache.GetUserPermissionsAsync(userId);

        if (permissions.Contains(requirement.PermissionCode, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}