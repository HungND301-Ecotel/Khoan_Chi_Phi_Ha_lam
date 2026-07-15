using Application.Catalog.Index.FixedKeys.Commands;
using Application.Catalog.Index.FixedKeys.Queries;
using Application.Catalog.Users.Commands;
using Application.Catalog.Users.Queries;
using Application.Dto.Authorization.Permission;
using Application.Dto.Catalog.FixedKey;
using Host.Controllers.Base;
using Infrastructure.Auth.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Systems;

public class SystemController : BaseAuthController
{
    #region FixedKey

    [HttpGet("FixedKey")]
    [OpenApiOperation("Get All FixedKey", "")]
    [HasPermission("system.fixkey.read","Hệ thống","Khóa cấu hình")]
    public async Task<IActionResult> GetAllFixedKey([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllFixedKeyQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("FixedKey/{id:guid}")]
    [OpenApiOperation("Get FixedKey By Id", "")]
    [HasPermission("system.fixkey.read", "Hệ thống", "Khóa cấu hình")]

    public async Task<IActionResult> GetFixedKeyById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetFixedKeyByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("FixedKey")]
    [OpenApiOperation("Update FixedKey", "")]
    [HasPermission("system.fixkey.update", "Hệ thống", "Khóa cấu hình")]

    public async Task<IActionResult> UpdateFixedKey([FromBody] FixedKeyDto updateModel)
    {
        var result = await Mediator.Send(new UpdateFixedKeyCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    #endregion

    #region Permission

    [HttpGet("permission/catalog")]
    [OpenApiOperation("Get Permission Catalog", "")]
    [HasPermission("system.permission.read", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> GetPermissionCatalogAsync()
    {
        var result = await Mediator.Send(new GetPermissionCatalogQuery());
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("permission/department/{departmentId:guid}")]
    [OpenApiOperation("Get Department Permissions", "")]
    [HasPermission("system.permission.read", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> GetDepartmentPermissionsAsync([FromRoute] Guid departmentId)
    {
        var result = await Mediator.Send(new GetDepartmentPermissionsQuery(departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("permission/position/{positionId:int}")]
    [OpenApiOperation("Get Position Permissions", "")]
    [HasPermission("system.permission.read", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> GetPositionPermissionsAsync([FromRoute] int positionId)
    {
        var result = await Mediator.Send(new GetPositionPermissionsQuery(positionId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("permission/user-override/{userId:int}")]
    [OpenApiOperation("Get User Override Permissions", "")]
    [HasPermission("system.permission.read", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> GetUserOverridePermissionsAsync([FromRoute] int userId)
    {
        var result = await Mediator.Send(new GetUserOverridePermissionsQuery(userId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("permission")]
    [OpenApiOperation("Get Current User Permissions", "")]
    public async Task<IActionResult> GetCurrentUserPermissionAsync()
    {
        var result = await Mediator.Send(new GetCurrentUserPermissionQuery());
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    [HttpPost("permission/department")]
    [OpenApiOperation("Update Department Permissions", "")]
    [HasPermission("system.permission.update", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> UpdateDepartmentPermissions([FromBody] UpdateDepartmentPermissionsDto dto)
    {
        var result = await Mediator.Send(new UpdateDepartmentPermissionsCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    [HttpPost("permission/position")]
    [OpenApiOperation("Update Position Permissions", "")]
    [HasPermission("system.permission.update", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> UpdatePositionPermissions([FromBody] UpdatePositionPermissionsDto dto)
    {
        var result = await Mediator.Send(new UpdatePositionPermissionsCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    [HttpPost("permission/user-override")]
    [OpenApiOperation("Update User Override Permissions", "")]
    [HasPermission("system.permission.update", "Hệ thống", "Phân quyền")]
    public async Task<IActionResult> UpdateUserOverridePermissions([FromBody] UpdateUserOverridePermissionsDto dto)
    {
        var result = await Mediator.Send(new UpdateUserOverridePermissionsCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    #endregion
}