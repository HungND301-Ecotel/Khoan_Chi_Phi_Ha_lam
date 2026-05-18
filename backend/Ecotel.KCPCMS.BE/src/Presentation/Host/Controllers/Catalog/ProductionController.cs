using Application.Catalog.Production.AcceptanceReports.Commands;
using Application.Catalog.Production.AcceptanceReports.Queries;
using Application.Catalog.Production.LongTermAnchorSeeds.Commands;
using Application.Catalog.Production.LongTermAnchorSeeds.Queries;
using Application.Catalog.Production.ProductionOutputs.Commands;
using Application.Catalog.Production.ProductionOutputs.Queries;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Dto.Catalog.ProductionOutput;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class ProductionController : BaseNoAuthController
{
    #region ProductionOutput

    [HttpGet("ProductionOutput")]
    [OpenApiOperation("Get All ProductionOutput", "")]
    public async Task<IActionResult> GetAllProductionOutput([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionOutputsQuery(pageIndex, pageSize, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOutput/{id:guid}")]
    [OpenApiOperation("Get ProductionOutput By Id", "")]
    public async Task<IActionResult> GetProductionOutputById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionOutputByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOutput/{id:guid}/detail")]
    [OpenApiOperation("Get ProductionOutput Detail", "Get production output with all acceptance report items grouped by category and material group")]
    public async Task<IActionResult> GetProductionOutputDetail([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionOutputDetailQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("ProductionOutput")]
    [OpenApiOperation("Create New ProductionOutput", "")]
    public async Task<IActionResult> CreateProductionOutput([FromBody] CreateProductionOutputDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionOutputCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProductionOutput")]
    [OpenApiOperation("Update ProductionOutput", "")]
    public async Task<IActionResult> UpdateProductionOutput([FromBody] ProductionOutputDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionOutputCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("ProductionOutputList")]
    [OpenApiOperation("Create ProductionOutput List", "")]
    public async Task<IActionResult> CreateProductionOutputList([FromBody] IList<CreateProductionOutputDto> createModels)
    {
        var result = await Mediator.Send(new CreateProductionOutputListCommand(createModels));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProductionOutputList")]
    [OpenApiOperation("Update ProductionOutput List", "")]
    public async Task<IActionResult> UpdateProductionOutputList([FromBody] IList<ProductionOutputDto> updateModels)
    {
        var result = await Mediator.Send(new UpdateProductionOutputListCommand(updateModels));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionOutput/{deleteId:guid}")]
    [OpenApiOperation("Delete ProductionOutput", "")]
    public async Task<IActionResult> DeleteProductionOutput([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductionOutputCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }


    [HttpDelete("ProductionOutput")]
    [OpenApiOperation("Delete ProductionOutput List", "")]
    public async Task<IActionResult> DeleteProductionOutputList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionOutputListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AcceptanceReport

    [HttpPost("AcceptanceReport/UploadFile/{productionOutputId}")]
    [OpenApiOperation("Upload AcceptanceReport File", "Upload Excel file to process acceptance report items")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadAcceptanceReportFile([FromForm] IFormFile file, [FromRoute] Guid productionOutputId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(CustomResponseMessage.FileEmpty);
        }

        var result = await Mediator.Send(new UploadAcceptanceReportFileCommand(file, productionOutputId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AcceptanceReport/{id:guid}")]
    [OpenApiOperation("Get AcceptanceReport By Id", "Get detail of acceptance report with items")]
    public async Task<IActionResult> GetAcceptanceReportById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAcceptanceReportByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AcceptanceReport")]
    [OpenApiOperation("Create AcceptanceReport", "Create new acceptance report with items")]
    public async Task<IActionResult> CreateAcceptanceReport([FromBody] CreateAcceptanceReportDto createModel)
    {
        var result = await Mediator.Send(new CreateAcceptanceReportCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AcceptanceReport")]
    [OpenApiOperation("Update AcceptanceReport", "Update new acceptance report with items")]
    public async Task<IActionResult> UpdateAcceptanceReport([FromBody] UpdateAcceptanceReportDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAcceptanceReportCommand(updateModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("AcceptanceReport/{id:guid}/download")]
    [OpenApiOperation("Download AcceptanceReport Excel", "Download excel file for acceptance report")]
    public async Task<IActionResult> DownloadAcceptanceReportExcel([FromRoute] Guid id)
    {
        var excelBytes = await Mediator.Send(new DownloadAcceptanceReportExcelQuery(id));
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AcceptanceReport_{id:N}.xlsx");
    }

    [HttpGet("AcceptanceReport/{id:guid}/additional-cost")]
    [OpenApiOperation("Get All Additional Costs", "Get all additional cost items grouped by type")]
    public async Task<IActionResult> GetAllAcceptanceReportAdditionalCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAllAcceptanceReportAdditionalCostQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    [HttpGet("AcceptanceReport/export")]
    [OpenApiOperation("Export AcceptanceReport By Period", "Export acceptance report excel by month and year")]
    public async Task<IActionResult> ExportAcceptanceReportByPeriod([FromQuery] string? month, [FromQuery] string? year)
    {
        var result = await Mediator.Send(new ExportAcceptanceReportByPeriodExcelQuery(month, year));
        return File(result.FileBytes.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }


    [HttpGet("AcceptanceReport/{id:guid}/long-term-tracking")]
    [OpenApiOperation("Get Long-term Item Tracking", "Get all long-term items with tracking logs (TH1 & TH2)")]
    public async Task<IActionResult> GetAllAcceptanceReportItemLog([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAllAcceptanceReportItemLogQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AcceptanceReport/{id:guid}/long-term-tracking/detail")]
    [OpenApiOperation("Get Detail Long-term Material Cost", "Get detail long-term material cost with latest tracking information")]
    public async Task<IActionResult> GetDetailLongTermTracking([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetDetailLongTermTrackingQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AcceptanceReport/{acceptanceReportId:guid}/export-longterm-material-cost")]
    [OpenApiOperation("Export Long-term Material Cost Excel", "Export long-term material cost accounting report in Excel format")]
    public async Task<IActionResult> ExportLongTermMaterialCostExcel(
        [FromRoute] Guid acceptanceReportId,
        [FromQuery] string? month,
        [FromQuery] string? year,
        [FromQuery] Guid? processGroupId)
    {
        var result = await Mediator.Send(new ExportLongTermMaterialCostExcelQuery(acceptanceReportId, month, year, processGroupId));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    [HttpPut("AcceptanceReport/long-term-tracking")]
    [OpenApiOperation("Update Allocation Ratio", "Update allocation ratio and recalculate log values")]
    public async Task<IActionResult> UpdateAcceptanceReportItemLog([FromBody] UpdateAcceptanceReportItemLogDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAcceptanceReportItemLogCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPut("AcceptanceReport/long-term-tracking/list")]
    [OpenApiOperation("Update Allocation Ratio List", "Update allocation ratio for multiple items and recalculate log values")]
    public async Task<IActionResult> UpdateAcceptanceReportItemLogList([FromBody] IList<UpdateAcceptanceReportItemLogDto> updateModels)
    {
        var result = await Mediator.Send(new UpdateAcceptanceReportItemLogListCommand(updateModels));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("AcceptanceReport/sctx-revenue-by-equipment")]
    [OpenApiOperation("Get SCTX Revenue By AssignmentCode", "Get monthly SCTX revenue for one assignment code")]
    public async Task<IActionResult> GetSctxRevenueByEquipment([FromBody] GetSctxEquipmentRevenueRequest request)
    {
        var assignmentCodeId = request.AssignmentCodeId ?? request.EquipmentId
            ?? throw new ArgumentException("AssignmentCodeId or EquipmentId is required");

        var result = await Mediator.Send(new GetSctxEquipmentRevenueByYearQuery(
            assignmentCodeId,
            request.DepartmentId,
            request.FromMonth,
            request.ToMonth));

        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Department/{departmentId:guid}/long-term-anchor-seed")]
    [OpenApiOperation("Get Long-term Anchor Seed Detail", "Get department long-term anchor seed detail")]
    public async Task<IActionResult> GetLongTermAnchorSeedDetail([FromRoute] Guid departmentId)
    {
        var result = await Mediator.Send(new GetLongTermAnchorSeedDetailQuery(departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("Department/long-term-anchor-seed")]
    [OpenApiOperation("Update Long-term Anchor Seed", "Update department long-term anchor seed items")]
    public async Task<IActionResult> UpdateLongTermAnchorSeed([FromBody] UpdateLongTermAnchorSeedRequestDto request)
    {
        var result = await Mediator.Send(new UpdateLongTermAnchorSeedCommand(request));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("Department/{departmentId:guid}/long-term-anchor-seed/upload-file")]
    [OpenApiOperation("Upload Long-term Anchor Seed File", "Upload department long-term anchor seed excel file")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadLongTermAnchorSeedFile([FromForm] IFormFile file, [FromRoute] Guid departmentId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(CustomResponseMessage.FileEmpty);
        }

        var result = await Mediator.Send(new UploadLongTermAnchorSeedFileCommand(file, departmentId));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpGet("Department/{departmentId:guid}/long-term-anchor-seed/export")]
    [OpenApiOperation("Export Long-term Anchor Seed File", "Export department long-term anchor seed excel file")]
    public async Task<IActionResult> ExportLongTermAnchorSeedFile([FromRoute] Guid departmentId)
    {
        var result = await Mediator.Send(new ExportLongTermAnchorSeedExcelQuery(departmentId));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    #endregion
}

