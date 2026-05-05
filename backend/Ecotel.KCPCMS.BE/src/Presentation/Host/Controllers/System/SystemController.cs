using Application.Catalog.Index.FixedKeys.Commands;
using Application.Catalog.Index.FixedKeys.Queries;
using Application.Dto.Catalog.FixedKey;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Systems;

public class SystemController : BaseNoAuthController
{
    #region FixedKey

    [HttpGet("FixedKey")]
    [OpenApiOperation("Get All FixedKey", "")]
    public async Task<IActionResult> GetAllFixedKey([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllFixedKeyQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("FixedKey/{id:guid}")]
    [OpenApiOperation("Get FixedKey By Id", "")]
    public async Task<IActionResult> GetFixedKeyById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetFixedKeyByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("FixedKey")]
    [OpenApiOperation("Update FixedKey", "")]
    public async Task<IActionResult> UpdateFixedKey([FromBody] FixedKeyDto updateModel)
    {
        var result = await Mediator.Send(new UpdateFixedKeyCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    #endregion
}