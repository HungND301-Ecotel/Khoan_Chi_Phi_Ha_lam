using Application.Catalog.Dashboard.Queries;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class DashboardController : BaseNoAuthController
{
    [HttpGet("cost-summary")]
    [OpenApiOperation("Get cost summary", "")]
    public async Task<IActionResult> GetCostSummary([FromQuery] Guid? processGroupId, [FromQuery] int year)
    {
        var result = await Mediator.Send(new GetCostSummaryQuery(processGroupId, year));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
}
