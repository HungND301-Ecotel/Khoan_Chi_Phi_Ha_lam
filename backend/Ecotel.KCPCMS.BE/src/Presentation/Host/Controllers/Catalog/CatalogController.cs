using Application.Catalog.Index.AdjustmentFactor.Commands;
using Application.Catalog.Index.AdjustmentFactor.Queries;
using Application.Catalog.Index.AdjustmentFactorDescription.Commands;
using Application.Catalog.Index.AdjustmentFactorDescription.Queries;
using Application.Catalog.Index.AkFactorConfig.Commands;
using Application.Catalog.Index.AkFactorConfig.Queries;
using Application.Catalog.Index.AssignmentCodes.Commands;
using Application.Catalog.Index.AssignmentCodes.Queries;
using Application.Catalog.Index.CargoType.Commands;
using Application.Catalog.Index.CargoType.Queries;
using Application.Catalog.Index.CuttingThickness.Commands;
using Application.Catalog.Index.CuttingThickness.Queries;
using Application.Catalog.Index.Department.Commands;
using Application.Catalog.Index.Department.Queries;
using Application.Catalog.Index.LongwallParameters.Commands;
using Application.Catalog.Index.LongwallParameters.Queries;
using Application.Catalog.Index.Material.Commands;
using Application.Catalog.Index.Material.Queries;
using Application.Catalog.Index.Metrics.Commands;
using Application.Catalog.Index.Metrics.Queries;
using Application.Catalog.Index.Passport.Commands;
using Application.Catalog.Index.Passport.Queries;
using Application.Catalog.Index.Position.Commands;
using Application.Catalog.Index.Position.Queries;
using Application.Catalog.Index.ProcessGroups.Commands;
using Application.Catalog.Index.ProcessGroups.Queries;
using Application.Catalog.Index.Product.Commands;
using Application.Catalog.Index.Product.Queries;
using Application.Catalog.Index.ProductionOrder.Commands;
using Application.Catalog.Index.ProductionOrder.Queries;
using Application.Catalog.Index.ProductionProcess.Commands;
using Application.Catalog.Index.ProductionProcess.Queries;
using Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;
using Application.Catalog.Index.RevenueCostAdjustmentConfig.Queries;
using Application.Catalog.Index.SavingsRateConfig.Commands;
using Application.Catalog.Index.SavingsRateConfig.Queries;
using Application.Catalog.Index.StoneClampRatio.Commands;
using Application.Catalog.Index.StoneClampRatio.Queries;
using Application.Catalog.Index.TransportLocation.Commands;
using Application.Catalog.Index.TransportLocation.Queries;
using Application.Catalog.Index.TransportRoutes.Commands;
using Application.Catalog.Index.TransportRoutes.Queries;
using Application.Catalog.Index.UnitOfMeasures.Commands;
using Application.Catalog.Index.UnitOfMeasures.Queries;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.AkFactorConfig;
using Application.Dto.Catalog.AssignmentCode;
using Application.Dto.Catalog.CargoType;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.Department;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.Material;
using Application.Dto.Catalog.Metric;
using Application.Dto.Catalog.NormFactor;
using Application.Dto.Catalog.Passport;
using Application.Dto.Catalog.Position;
using Application.Dto.Catalog.ProcessGroup;
using Application.Dto.Catalog.Product;
using Application.Dto.Catalog.ProductionOrder;
using Application.Dto.Catalog.ProductionProcess;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Application.Dto.Catalog.SavingsRateConfig;
using Application.Dto.Catalog.StoneClampRatio;
using Application.Dto.Catalog.TransportLocation;
using Application.Dto.Catalog.TransportRoute;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Host.Controllers.Base;
using Infrastructure.Auth.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Authorization;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class CatalogController : BaseAuthController
{
    #region UnitOfMeasure

    [HttpGet("UnitOfMeasure")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Read)]
    public async Task<IActionResult> GetAllUnitOfMeasure([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllUnitOfMeasureQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("UnitOfMeasure/{id:guid}")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Read)]
    public async Task<IActionResult> GetUnitOfMeasureById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("UnitOfMeasure")]
    [OpenApiOperation("Create New UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Create)]
    public async Task<IActionResult> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureDto createModel)
    {
        var result = await Mediator.Send(new CreateUnitOfMeasureCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("UnitOfMeasure/export")]
    [OpenApiOperation("Export UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Export)]
    public async Task<IActionResult> ExportUnitOfMeasure()
    {
        var fileByte = await Mediator.Send(new ExportExcelUnitOfMeasureQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "don_vi_tinh.xlsx");
        return result;
    }

    [HttpPost("UnitOfMeasure/import")]
    [OpenApiOperation("Import UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Import)]
    public async Task<IActionResult> ImportUnitOfMeasure([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportUnitOfMeasureExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPut("UnitOfMeasure")]
    [OpenApiOperation("Update UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Update)]
    public async Task<IActionResult> UpdateUnitOfMeasure([FromBody] UnitOfMeasureDto updateModel)
    {
        var result = await Mediator.Send(new UpdateUnitOfMeasureCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("UnitOfMeasure/{deleteId:guid}")]
    [OpenApiOperation("Delete UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Delete)]
    public async Task<IActionResult> DeleteUnitOfMeasure([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("UnitOfMeasure")]
    [OpenApiOperation("Delete Many UnitOfMeasure", "")]
    [HasPermission(AppPermissions.Catalog.UnitOfMeasure.Delete)]
    public async Task<IActionResult> DeleteUnitOfMeasureList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region AssignmentCode

    [HttpGet("AssignmentCode")]
    [OpenApiOperation("Get All AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Read)]
    public async Task<IActionResult> GetAllAssignmentCode([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAssignmentCodeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AssignmentCode/export")]
    [OpenApiOperation("Export AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Export)]
    public async Task<IActionResult> ExportAssignmentCode()
    {
        var fileByte = await Mediator.Send(new ExportExcelAssignmentCodeQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ma_giao_khoan.xlsx");
        return result;
    }

    [HttpPost("AssignmentCode/import")]
    [OpenApiOperation("Import AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Import)]
    public async Task<IActionResult> ImportAssignmentCode([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAssignmentCodeExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AssignmentCode/{id:guid}")]
    [OpenApiOperation("Get AssignmentCode by Id", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Read)]
    public async Task<IActionResult> GetAssignmentCodeById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAssignmentCodeDetailByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AssignmentCode")]
    [OpenApiOperation("Create New AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Create)]
    public async Task<IActionResult> CreateAssignmentCode([FromBody] CreateAssignmentCodeDto createModel)
    {
        var result = await Mediator.Send(new CreateAssignmentCodeCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AssignmentCode")]
    [OpenApiOperation("Update AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Update)]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateAssignmentCodeDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAssignmentCodeCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AssignmentCode/{deleteId:guid}")]
    [OpenApiOperation("Delete AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Delete)]
    public async Task<IActionResult> DeleteAssignmentCode([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AssignmentCode")]
    [OpenApiOperation("Delete Many AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.AssignmentCode.Delete)]
    public async Task<IActionResult> DeleteAssignmentCodeList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Department

    [HttpGet("Department")]
    [OpenApiOperation("Get All Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Read)]
    public async Task<IActionResult> GetAllDepartment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllDepartmentQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Department/export")]
    [OpenApiOperation("Export Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Export)]
    public async Task<IActionResult> ExportDepartment()
    {
        var fileByte = await Mediator.Send(new ExportExcelDepartmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Don_vi.xlsx");
        return result;
    }

    [HttpPost("Department/import")]
    [OpenApiOperation("Import Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Import)]
    public async Task<IActionResult> ImportDepartment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportDepartmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Department/{id:guid}")]
    [OpenApiOperation("Get Department By Id", "")]
    [HasPermission(AppPermissions.Catalog.Department.Read)]
    public async Task<IActionResult> GetDepartmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetDepartmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Department")]
    [OpenApiOperation("Create New Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Create)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto createModel)
    {
        var result = await Mediator.Send(new CreateDepartmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Department")]
    [OpenApiOperation("Update Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Update)]
    public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdateDepartmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Department/{deleteId:guid}")]
    [OpenApiOperation("Delete Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Delete)]
    public async Task<IActionResult> DeleteDepartment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteDepartmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Department")]
    [OpenApiOperation("Delete Many Department", "")]
    [HasPermission(AppPermissions.Catalog.Department.Delete)]
    public async Task<IActionResult> DeleteDepartmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteDepartmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region Material

    [HttpGet("Material")]
    [OpenApiOperation("Get All Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Read)]
    public async Task<IActionResult> GetAllMaterial([FromQuery] MaterialType? materialType, [FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var normalizedMaterialType = materialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : materialType;
        var result = await Mediator.Send(new GetAllMaterialQuery(pageIndex, pageSize, search, ignorePagination, normalizedMaterialType, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Material/export")]
    [OpenApiOperation("Export Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Export)]
    public async Task<IActionResult> ExportMaterial([FromQuery] MaterialType materialType = MaterialType.MaterialInContract)
    {
        var normalizedMaterialType = materialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : materialType;
        var fileByte = await Mediator.Send(new ExportExcelMaterialQuery(normalizedMaterialType));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vat_tu_tai_san.xlsx");
        return result;
    }

    [HttpPost("Material/import")]
    [OpenApiOperation("Import Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Import)]
    public async Task<IActionResult> ImportMaterial([FromForm] ImportMaterialDto importModel)
    {
        var normalizedMaterialType = importModel.MaterialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : importModel.MaterialType;
        var result = await Mediator.Send(new ImportMaterialExcelCommand(importModel.FormFile, normalizedMaterialType));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Material/{id:guid}")]
    [OpenApiOperation("Get Material by Id", "")]
    [HasPermission(AppPermissions.Catalog.Material.Read)]
    public async Task<IActionResult> GetMaterialById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Material")]
    [OpenApiOperation("Create New Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Create)]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialDto createModel)
    {
        var result = await Mediator.Send(new CreateMaterialCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Material")]
    [OpenApiOperation("Update AssignmentCode", "")]
    [HasPermission(AppPermissions.Catalog.Material.Update)]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateMaterialDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMaterialCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Material/{deleteId:guid}")]
    [OpenApiOperation("Delete Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Delete)]
    public async Task<IActionResult> DeleteMaterial([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Material")]
    [OpenApiOperation("Delete Many Material", "")]
    [HasPermission(AppPermissions.Catalog.Material.Delete)]
    public async Task<IActionResult> DeleteMaterialList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Position
    [HttpGet("Position")]
    [OpenApiOperation("Get All Position", "")]
    [HasPermission(AppPermissions.Catalog.Position.Read)]
    public async Task<IActionResult> GetAllPosition(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = "",
        [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllPositionQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Position/{id:int}")]
    [OpenApiOperation("Get Position by Id", "")]
    [HasPermission(AppPermissions.Catalog.Position.Read)]

    public async Task<IActionResult> GetPositionById([FromRoute] int id)
    {
        var result = await Mediator.Send(new GetPositionByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Position")]
    [OpenApiOperation("Create Position", "")]
    [HasPermission(AppPermissions.Catalog.Position.Create)]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionDto dto)
    {
        var result = await Mediator.Send(new CreatePositionCommand(dto));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Position")]
    [OpenApiOperation("Update Position", "")]
    [HasPermission(AppPermissions.Catalog.Position.Update)]
    public async Task<IActionResult> UpdatePosition([FromBody] UpdatePositionDto dto)
    {
        var result = await Mediator.Send(new UpdatePositionCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Position/{id:int}")]
    [OpenApiOperation("Delete Position", "")]
    [HasPermission(AppPermissions.Catalog.Position.Delete)]
    public async Task<IActionResult> DeletePosition([FromRoute] int id)
    {
        var result = await Mediator.Send(new DeletePositionCommand(id));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Position/list")]
    [OpenApiOperation("Delete Position List", "")]
    [HasPermission(AppPermissions.Catalog.Position.Delete)]

    public async Task<IActionResult> DeletePositionList([FromBody] IList<int> deleteIds)
    {
        var result = await Mediator.Send(new DeletePositionListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("Position/export")]
    [OpenApiOperation("Export Position List To Excel", "")]
    [HasPermission(AppPermissions.Catalog.Position.Export)]

    public async Task<IActionResult> ExportPosition()
    {
        var fileBytes = await Mediator.Send(new ExportExcelPositionQuery());
        var fileName = $"danh-sach-chuc-vu-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("Position/import")]
    [Consumes("multipart/form-data")]
    [HasPermission(AppPermissions.Catalog.Position.Import)]

    public async Task<IActionResult> ImportPosition([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportExcelPositionCommand(importModel.FormFile));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    #endregion


    #region ProcessGroup

    [HttpGet("ProcessGroup")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Read)]
    public async Task<IActionResult> GetAllProcessGroup([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProcessGroupQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProcessGroup/export")]
    [OpenApiOperation("Export ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Export)]
    public async Task<IActionResult> ExportProcessGroup()
    {
        var fileByte = await Mediator.Send(new ExportExcelProcessGroupQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Nhom_cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProcessGroup/import")]
    [OpenApiOperation("Import ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Import)]
    public async Task<IActionResult> ImportProcessGroup([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProcessGroupExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("ProcessGroup/{Id:guid}")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Read)]
    public async Task<IActionResult> GetProcessGroupById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProcessGroupByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("ProcessGroup")]
    [OpenApiOperation("Create New ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Create)]
    public async Task<IActionResult> CreateProcessGroup([FromBody] CreateProcessGroupDto createModel)
    {
        var result = await Mediator.Send(new CreateProcessGroupCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProcessGroup")]
    [OpenApiOperation("Update ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Update)]
    public async Task<IActionResult> UpdateProcessGroup([FromBody] UpdateProcessGroupDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProcessGroupCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProcessGroup/{deleteId:guid}")]
    [OpenApiOperation("Delete ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Delete)]
    public async Task<IActionResult> DeleteProcessGroup([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProcessGroupCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProcessGroup")]
    [OpenApiOperation("Delete Many ProcessGroup", "")]
    [HasPermission(AppPermissions.Catalog.ProcessGroup.Delete)]
    public async Task<IActionResult> DeleteProcessGroupList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProcessGroupListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region TransportRoute

    [HttpGet("TransportRoute")]
    [OpenApiOperation("Get All TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Read)]
    public async Task<IActionResult> GetAllTransportRoute([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTransportRouteQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TransportRoute/{id:guid}")]
    [OpenApiOperation("Get TransportRoute By Id", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Read)]
    public async Task<IActionResult> GetTransportRouteById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTransportRouteByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TransportRoute")]
    [OpenApiOperation("Create New TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Create)]
    public async Task<IActionResult> CreateTransportRoute([FromBody] CreateTransportRouteDto createModel)
    {
        var result = await Mediator.Send(new CreateTransportRouteCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("TransportRoute")]
    [OpenApiOperation("Update TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Update)]
    public async Task<IActionResult> UpdateTransportRoute([FromBody] TransportRouteDto updateModel)
    {
        var result = await Mediator.Send(new UpdateTransportRouteCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TransportRoute/{deleteId:guid}")]
    [OpenApiOperation("Delete TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Delete)]
    public async Task<IActionResult> DeleteTransportRoute([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTransportRouteCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TransportRoute")]
    [OpenApiOperation("Delete Many TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Delete)]
    public async Task<IActionResult> DeleteTransportRouteList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteTransportRouteListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TransportRoute/export")]
    [OpenApiOperation("Export TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Export)]
    public async Task<IActionResult> ExportTransportRoute()
    {
        var fileByte = await Mediator.Send(new ExportExcelTransportRouteQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Tuyen_van_tai.xlsx");
        return result;
    }

    [HttpPost("TransportRoute/import")]
    [OpenApiOperation("Import TransportRoute", "")]
    [HasPermission(AppPermissions.Catalog.TransportRoute.Import)]
    public async Task<IActionResult> ImportTransportRoute([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTransportRouteExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region ProductionProcess

    [HttpGet("ProductionProcess")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Read)]
    public async Task<IActionResult> GetAllProductionProcess([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionProcessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionProcess/export")]
    [OpenApiOperation("Export ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Export)]
    public async Task<IActionResult> ExportProductionProcess()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductionProcessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProductionProcess/import")]
    [OpenApiOperation("Import ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Import)]
    public async Task<IActionResult> ImportProductionProcess([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductionProcessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    // check api fe 
    [HttpPost("ProductionProcess")]
    [OpenApiOperation("Create New ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Create)]
    public async Task<IActionResult> CreateProductionProcess([FromBody] CreateProductionProcessDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionProcessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("ProductionProcess/{Id:guid}")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Read)]
    public async Task<IActionResult> GetProductionProcessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionProcessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ProductionProcess")]
    [OpenApiOperation("Update ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Update)]
    public async Task<IActionResult> UpdateProductionProcess([FromBody] UpdateProductionProcessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionProcessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionProcess/{deleteId:guid}")]
    [OpenApiOperation("Delete ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Delete)]
    public async Task<IActionResult> DeleteProductionProcess([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductionProcessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProductionProcess")]
    [OpenApiOperation("Delete Many ProductionProcess", "")]
    [HasPermission(AppPermissions.Catalog.ProductionProcess.Delete)]
    public async Task<IActionResult> DeleteProductionProcessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionProcessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactor

    [HttpGet("AdjustmentFactor")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Read)]
    public async Task<IActionResult> GetAllAdjustmentFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/export")]
    [OpenApiOperation("Export AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Export)]
    public async Task<IActionResult> ExportAdjustmentFactor()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactor/import")]
    [OpenApiOperation("Import AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Import)]
    public async Task<IActionResult> ImportAdjustmentFactor([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactor/details")]
    [OpenApiOperation("Get All AdjustmentFactorDetail", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Read)]
    public async Task<IActionResult> GetAllAdjustmentFactorDetail([FromQuery] Guid? ProcessGroupId)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDetailQuery(ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/{id:guid}")]
    [OpenApiOperation("Get AdjustmentFactor By Id", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Read)]
    public async Task<IActionResult> GetAdjustmentFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactor")]
    [OpenApiOperation("Create New AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Create)]
    public async Task<IActionResult> CreateAdjustmentFactor([FromBody] CreateAdjustmentFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactor")]
    [OpenApiOperation("Update AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Update)]
    public async Task<IActionResult> UpdateAdjustmentFactor([FromBody] UpdateAdjustmentFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactor/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Delete)]
    public async Task<IActionResult> DeleteAdjustmentFactor([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactor")]
    [OpenApiOperation("Delete Many AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactor.Delete)]
    public async Task<IActionResult> DeleteAdjustmentFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactorDescription

    [HttpGet("AdjustmentFactorDescription")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Read)]
    public async Task<IActionResult> GetAllAdjustmentFactorDescription([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDescriptionQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/export")]
    [OpenApiOperation("Export AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Export)]
    public async Task<IActionResult> ExportAdjustmentFactorDescription()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorDescriptionQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dien_giai_he_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactorDescription/import")]
    [OpenApiOperation("Import AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Import)]
    public async Task<IActionResult> ImportAdjustmentFactorDescription([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorDescriptionExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/{id:guid}")]
    [OpenApiOperation("Get All AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Read)]
    public async Task<IActionResult> GetAdjustmentFactorDescriptionById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorDescriptionByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactorDescription")]
    [OpenApiOperation("Create New AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Create)]
    public async Task<IActionResult> CreateAdjustmentFactorDescription([FromBody] CreateAdjustmentFactorDescriptionDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorDescriptionCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactorDescription")]
    [OpenApiOperation("Update AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Update)]
    public async Task<IActionResult> UpdateAdjustmentFactorDescription([FromBody] UpdateAdjustmentFactorDescriptionDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorDescriptionCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Delete)]
    public async Task<IActionResult> DeleteAdjustmentFactorDescription([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription")]
    [OpenApiOperation("Delete Many AdjustmentFactorDescription", "")]
    [HasPermission(AppPermissions.Catalog.AdjustmentFactorDescription.Delete)]
    public async Task<IActionResult> DeleteAdjustmentFactorDescriptionList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Passport

    [HttpGet("Passport")]
    [OpenApiOperation("Get All Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Read)]
    public async Task<IActionResult> GetAllPassport([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllPassportQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Passport/export")]
    [OpenApiOperation("Export Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Export)]
    public async Task<IActionResult> ExportPassport()
    {
        var fileByte = await Mediator.Send(new ExportExcelPassportQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ho_Chieu_Sd_Sc.xlsx");
        return result;
    }

    [HttpPost("Passport/import")]
    [OpenApiOperation("Import Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Import)]
    public async Task<IActionResult> ImportPassport([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPassportExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Passport/{id:guid}")]
    [OpenApiOperation("Get Passport By Id", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Read)]
    public async Task<IActionResult> GetPassportById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPassportByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Passport")]
    [OpenApiOperation("Create New Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Create)]
    public async Task<IActionResult> CreatePassport([FromBody] CreatePassportDto createModel)
    {
        var result = await Mediator.Send(new CreatePassportCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Passport")]
    [OpenApiOperation("Update Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Update)]
    public async Task<IActionResult> UpdatePassport([FromBody] PassportDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePassportCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Passport/{deleteId:guid}")]
    [OpenApiOperation("Delete Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Delete)]
    public async Task<IActionResult> DeletePassport([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeletePassportCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Passport")]
    [OpenApiOperation("Delete Many Passport", "")]
    [HasPermission(AppPermissions.Catalog.Passport.Delete)]
    public async Task<IActionResult> DeletePassportList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePassportListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region LongwallParameters

    [HttpGet("LongwallParameters")]
    [OpenApiOperation("Get All LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Read)]
    public async Task<IActionResult> GetAllLongwallParameters([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallParametersQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("LongwallParameters/export")]
    [OpenApiOperation("Export LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Export)]
    public async Task<IActionResult> ExportLongwallParameters()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallParametersQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Thong_so_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallParameters/import")]
    [OpenApiOperation("Import LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Import)]
    public async Task<IActionResult> ImportLongwallParameters([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallParametersExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("LongwallParameters/{id:guid}")]
    [OpenApiOperation("Get LongwallParameters By Id", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Read)]
    public async Task<IActionResult> GetLongwallParametersById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallParametersByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallParameters")]
    [OpenApiOperation("Create New LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Create)]
    public async Task<IActionResult> CreateLongwallParameters([FromBody] CreateLongwallParametersDto createModel)
    {
        var result = await Mediator.Send(new CreateLongwallParametersCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("LongwallParameters")]
    [OpenApiOperation("Update LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Update)]
    public async Task<IActionResult> UpdateLongwallParameters([FromBody] LongwallParametersDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallParametersCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallParameters/{deleteId:guid}")]
    [OpenApiOperation("Delete LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Delete)]
    public async Task<IActionResult> DeleteLongwallParameters([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallParameters")]
    [OpenApiOperation("Delete Many LongwallParameters", "")]
    [HasPermission(AppPermissions.Catalog.LongwallParameters.Delete)]
    public async Task<IActionResult> DeleteLongwallParametersList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region CuttingThickness

    [HttpGet("CuttingThickness")]
    [OpenApiOperation("Get All CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Read)]
    public async Task<IActionResult> GetAllCuttingThickness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllCuttingThicknessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("CuttingThickness/export")]
    [OpenApiOperation("Export CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Export)]
    public async Task<IActionResult> ExportCuttingThickness()
    {
        var fileByte = await Mediator.Send(new ExportExcelCuttingThicknessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chieu_day_lop_khau.xlsx");
        return result;
    }

    [HttpPost("CuttingThickness/import")]
    [OpenApiOperation("Import CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Import)]
    public async Task<IActionResult> ImportCuttingThickness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportCuttingThicknessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("CuttingThickness/{id:guid}")]
    [OpenApiOperation("Get CuttingThickness By Id", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Read)]
    public async Task<IActionResult> GetCuttingThicknessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetCuttingThicknessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("CuttingThickness")]
    [OpenApiOperation("Create New CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Create)]
    public async Task<IActionResult> CreateCuttingThicknesss([FromBody] CreateCuttingThicknessDto createModel)
    {
        var result = await Mediator.Send(new CreateCuttingThicknessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("CuttingThickness")]
    [OpenApiOperation("Update CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Update)]
    public async Task<IActionResult> UpdateCuttingThickness([FromBody] CuttingThicknessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateCuttingThicknessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("CuttingThickness/{deleteId:guid}")]
    [OpenApiOperation("Delete CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Delete)]
    public async Task<IActionResult> DeleteCuttingThickness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("CuttingThickness")]
    [OpenApiOperation("Delete Many CuttingThickness", "")]
    [HasPermission(AppPermissions.Catalog.CuttingThickness.Delete)]
    public async Task<IActionResult> DeleteCuttingThicknessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Hardness

    [HttpGet("Hardness")]
    [OpenApiOperation("Get All Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Read)]
    public async Task<IActionResult> GetAllHardness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Hardness>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Hardness/export")]
    [OpenApiOperation("Export Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Export)]
    public async Task<IActionResult> ExportHardness()
    {
        var fileByte = await Mediator.Send(new ExportExcelHardnessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Do_kien_co_than_da.xlsx");
        return result;
    }

    [HttpPost("Hardness/import")]
    [OpenApiOperation("Import Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Import)]
    public async Task<IActionResult> ImportHardness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportHardnessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Hardness/{Id:guid}")]
    [OpenApiOperation("Get Hardness By Id", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Read)]
    public async Task<IActionResult> GetHardnessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Hardness>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Hardness")]
    [OpenApiOperation("Create New Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Create)]
    public async Task<IActionResult> CreateHardness([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Hardness>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Hardness")]
    [OpenApiOperation("Update Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Update)]
    public async Task<IActionResult> UpdateHardness([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Hardness>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Hardness/{deleteId:guid}")]
    [OpenApiOperation("Delete Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Delete)]
    public async Task<IActionResult> DeleteHardness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Hardness>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Hardness")]
    [OpenApiOperation("Delete Many Hardness", "")]
    [HasPermission(AppPermissions.Catalog.Hardness.Delete)]
    public async Task<IActionResult> DeleteHardnessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Hardness>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Power

    [HttpGet("Power")]
    [OpenApiOperation("Get All Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Read)]
    public async Task<IActionResult> GetAllPower([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Power>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Power/export")]
    [OpenApiOperation("Export Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Export)]
    public async Task<IActionResult> ExportPower()
    {
        var fileByte = await Mediator.Send(new ExportExcelPowerQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_suat.xlsx");
        return result;
    }

    [HttpPost("Power/import")]
    [OpenApiOperation("Import Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Import)]
    public async Task<IActionResult> ImportPower([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPowerExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Power/{Id:guid}")]
    [OpenApiOperation("Get Power By Id", "")]
    [HasPermission(AppPermissions.Catalog.Power.Read)]
    public async Task<IActionResult> GetPowerById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Power>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Power")]
    [OpenApiOperation("Create New Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Create)]
    public async Task<IActionResult> CreatePower([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Power>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Power")]
    [OpenApiOperation("Update Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Update)]
    public async Task<IActionResult> UpdatePower([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Power>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Power/{deleteId:guid}")]
    [OpenApiOperation("Delete Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Delete)]
    public async Task<IActionResult> DeletePower([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Power>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Power")]
    [OpenApiOperation("Delete Many Power", "")]
    [HasPermission(AppPermissions.Catalog.Power.Delete)]
    public async Task<IActionResult> DeletePowerList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Power>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region InsertItem

    [HttpGet("InsertItem")]
    [OpenApiOperation("Get All InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Read)]
    public async Task<IActionResult> GetAllInsertItem([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<InsertItem>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("InsertItem/export")]
    [OpenApiOperation("Export InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Export)]
    public async Task<IActionResult> ExportInsertItem()
    {
        var fileByte = await Mediator.Send(new ExportExcelInsertItemQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chen.xlsx");
        return result;
    }

    [HttpPost("InsertItem/import")]
    [OpenApiOperation("Import InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Import)]
    public async Task<IActionResult> ImportInsertItem([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportInsertItemExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("InsertItem/{id:guid}")]
    [OpenApiOperation("Get InsertItem By Id", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Read)]
    public async Task<IActionResult> GetInsertItemById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<InsertItem>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("InsertItem")]
    [OpenApiOperation("Create New InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Create)]
    public async Task<IActionResult> CreateInsertItem([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<InsertItem>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("InsertItem")]
    [OpenApiOperation("Update InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Update)]
    public async Task<IActionResult> UpdateInsertItem([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<InsertItem>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("InsertItem/{deleteId:guid}")]
    [OpenApiOperation("Delete InsertItem", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Delete)]
    public async Task<IActionResult> DeleteInsertItem([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<InsertItem>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("InsertItem")]
    [OpenApiOperation("Delete Many InsertItems", "")]
    [HasPermission(AppPermissions.Catalog.InsertItem.Delete)]
    public async Task<IActionResult> DeleteInsertItemList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<InsertItem>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductionOrder

    [HttpGet("ProductionOrder")]
    [OpenApiOperation("Get All ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Read)]
    public async Task<IActionResult> GetAllProductionOrder([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionOrderQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOrder/{id:guid}")]
    [OpenApiOperation("Get ProductionOrder By Id", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Read)]
    public async Task<IActionResult> GetProductionOrderById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionOrderByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOrder/export")]
    [OpenApiOperation("Export ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Export)]
    public async Task<IActionResult> ExportProductionOrder()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductionOrderQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Quyet_dinh_lenh_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProductionOrder/import")]
    [OpenApiOperation("Import ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Import)]
    public async Task<IActionResult> ImportProductionOrder([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductionOrderExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("ProductionOrder")]
    [OpenApiOperation("Create New ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Create)]
    public async Task<IActionResult> CreateProductionOrder([FromBody] CreateProductionOrderDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionOrderCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProductionOrder")]
    [OpenApiOperation("Update ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Update)]
    public async Task<IActionResult> UpdateProductionOrder([FromBody] ProductionOrderDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionOrderCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionOrder")]
    [OpenApiOperation("Delete Many ProductionOrder", "")]
    [HasPermission(AppPermissions.Catalog.ProductionOrder.Delete)]
    public async Task<IActionResult> DeleteProductionOrderList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionOrderListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Technology

    [HttpGet("Technology")]
    [OpenApiOperation("Get All Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Read)]
    public async Task<IActionResult> GetAllTechnology([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Technology>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Technology/export")]
    [OpenApiOperation("Export Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Export)]
    public async Task<IActionResult> ExportTechnology()
    {
        var fileByte = await Mediator.Send(new ExportExcelTechnologyQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_nghe.xlsx");
        return result;
    }

    [HttpPost("Technology/import")]
    [OpenApiOperation("Import Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Import)]
    public async Task<IActionResult> ImportTechnology([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTechnologyExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Technology/{id:guid}")]
    [OpenApiOperation("Get Technology By Id", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Read)]
    public async Task<IActionResult> GetTechnologyById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Technology>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Technology")]
    [OpenApiOperation("Create New Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Create)]
    public async Task<IActionResult> CreateTechnology([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Technology>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Technology")]
    [OpenApiOperation("Update Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Update)]
    public async Task<IActionResult> UpdateTechnology([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Technology>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Technology/{deleteId:guid}")]
    [OpenApiOperation("Delete Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Delete)]
    public async Task<IActionResult> DeleteTechnology([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Technology>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Technology")]
    [OpenApiOperation("Delete Many Technology", "")]
    [HasPermission(AppPermissions.Catalog.Technology.Delete)]
    public async Task<IActionResult> DeleteTechnologyList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Technology>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SeamFace

    [HttpGet("SeamFace")]
    [OpenApiOperation("Get All SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Read)]
    public async Task<IActionResult> GetAllSeamFace([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<SeamFace>(pageIndex, pageSize, search, ignorePagination));

        result.Data = result.Data
            .OrderBy(d => ExtractLeadingNumber(d.Value))
            .ThenBy(d => d.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(result, MessageCommon.GetDataSuccess);
    }

    private static double ExtractLeadingNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.MaxValue;
        }

        var match = System.Text.RegularExpressions.Regex.Match(value, @"\d+(\.\d+)?");
        return match.Success
            ? double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)
            : double.MaxValue;
    }

    [HttpGet("SeamFace/export")]
    [OpenApiOperation("Export SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Export)]
    public async Task<IActionResult> ExportSeamFace()
    {
        var fileByte = await Mediator.Send(new ExportExcelSeamFaceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mat_nut.xlsx");
        return result;
    }

    [HttpPost("SeamFace/import")]
    [OpenApiOperation("Import SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Import)]
    public async Task<IActionResult> ImportSeamFace([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSeamFaceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SeamFace/{id:guid}")]
    [OpenApiOperation("Get SeamFace By Id", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Read)]
    public async Task<IActionResult> GetSeamFaceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SeamFace>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SeamFace")]
    [OpenApiOperation("Create New SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Create)]
    public async Task<IActionResult> CreateSeamFace([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SeamFace>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SeamFace")]
    [OpenApiOperation("Update SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Update)]
    public async Task<IActionResult> UpdateSeamFace([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SeamFace>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SeamFace/{deleteId:guid}")]
    [OpenApiOperation("Delete SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Delete)]
    public async Task<IActionResult> DeleteSeamFace([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SeamFace>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SeamFace")]
    [OpenApiOperation("Delete Many SeamFace", "")]
    [HasPermission(AppPermissions.Catalog.SeamFace.Delete)]
    public async Task<IActionResult> DeleteSeamFaceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SeamFace>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region StoneClampRatio

    [HttpGet("StoneClampRatio")]
    [OpenApiOperation("Get All StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Read)]
    public async Task<IActionResult> GetAllStoneClampRatio([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllStoneClampRatioQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/{id:guid}")]
    [OpenApiOperation("Get StoneClampRatio By Id", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Read)]
    public async Task<IActionResult> GetStoneClampRatioById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetStoneClampRatioByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/export")]
    [OpenApiOperation("Export StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Export)]
    public async Task<IActionResult> ExportStoneClampRatio()
    {
        var fileByte = await Mediator.Send(new ExportExcelStoneClampRatioQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ti_le_da_kep.xlsx");
        return result;
    }

    [HttpPost("StoneClampRatio/import")]
    [OpenApiOperation("Import StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Import)]
    public async Task<IActionResult> ImportStoneClampRatio([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportStoneClampRatioExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("StoneClampRatio")]
    [OpenApiOperation("Create New StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Create)]
    public async Task<IActionResult> CreateStoneClampRatio([FromBody] CreateStoneClampRatioDto createModel)
    {
        var result = await Mediator.Send(new CreateStoneClampRatioCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("StoneClampRatio")]
    [OpenApiOperation("Update StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Update)]
    public async Task<IActionResult> UpdateStoneClampRatio([FromBody] UpdateStoneClampRatioDto updateModel)
    {
        var result = await Mediator.Send(new UpdateStoneClampRatioCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("StoneClampRatio/{deleteId:guid}")]
    [OpenApiOperation("Delete StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Delete)]
    public async Task<IActionResult> DeleteStoneClampRatio([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("StoneClampRatio")]
    [OpenApiOperation("Delete Many StoneClampRatio", "")]
    [HasPermission(AppPermissions.Catalog.StoneClampRatio.Delete)]
    public async Task<IActionResult> DeleteStoneClampRatioList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SupportStep

    [HttpGet("SupportStep")]
    [OpenApiOperation("Get All SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Read)]
    public async Task<IActionResult> GetAllSupportStep([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<SupportStep>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SupportStep/export")]
    [OpenApiOperation("Export SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Export)]
    public async Task<IActionResult> ExportSupportStep()
    {
        var fileByte = await Mediator.Send(new ExportExcelSupportStepQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Buoc_chong.xlsx");
        return result;
    }

    [HttpPost("SupportStep/import")]
    [OpenApiOperation("Import SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Import)]
    public async Task<IActionResult> ImportSupportStep([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSupportStepExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SupportStep/{id:guid}")]
    [OpenApiOperation("Get SupportStep By Id", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Read)]
    public async Task<IActionResult> GetSupportStepById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SupportStep>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SupportStep")]
    [OpenApiOperation("Create New SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Create)]
    public async Task<IActionResult> CreateSupportStep([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SupportStep>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SupportStep")]
    [OpenApiOperation("Update SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Update)]
    public async Task<IActionResult> UpdateSupportStep([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SupportStep>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SupportStep/{deleteId:guid}")]
    [OpenApiOperation("Delete SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Delete)]
    public async Task<IActionResult> DeleteSupportStep([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SupportStep>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SupportStep")]
    [OpenApiOperation("Delete Many SupportStep", "")]
    [HasPermission(AppPermissions.Catalog.SupportStep.Delete)]
    public async Task<IActionResult> DeleteSupportStepList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SupportStep>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Product

    [HttpGet("Product")]
    [OpenApiOperation("Get All Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Read)]
    public async Task<IActionResult> GetAllProduct([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Product/export")]
    [OpenApiOperation("Export Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Export)]
    public async Task<IActionResult> ExportProduct()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "San_pham.xlsx");
        return result;
    }

    [HttpPost("Product/import")]
    [OpenApiOperation("Import Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Import)]
    public async Task<IActionResult> ImportProduct([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Product/{id:guid}")]
    [OpenApiOperation("Get Product By Id", "")]
    [HasPermission(AppPermissions.Catalog.Product.Read)]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Product")]
    [OpenApiOperation("Create New Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Create)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createModel)
    {
        var result = await Mediator.Send(new CreateProductCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Product")]
    [OpenApiOperation("Update Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Update)]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Product/{deleteId:guid}")]
    [OpenApiOperation("Delete Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Delete)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Product")]
    [OpenApiOperation("Delete Many Product", "")]
    [HasPermission(AppPermissions.Catalog.Product.Delete)]
    public async Task<IActionResult> DeleteProductList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region NormFactor

    [HttpGet("NormFactor")]
    [OpenApiOperation("Get All NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Read)]
    public async Task<IActionResult> GetAllNormFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllNormFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("NormFactor/{id:guid}")]
    [OpenApiOperation("Get NormFactor By Id", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Read)]
    public async Task<IActionResult> GetNormFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetNormFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("NormFactor/export")]
    [OpenApiOperation("Export NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Export)]
    public async Task<IActionResult> ExportNormFactor()
    {
        var fileByte = await Mediator.Send(new ExportExcelNormFactorQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("NormFactor/import")]
    [OpenApiOperation("Import NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Import)]
    public async Task<IActionResult> ImportNormFactor([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportNormFactorExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("NormFactor")]
    [OpenApiOperation("Create New NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Create)]
    public async Task<IActionResult> CreateNormFactor([FromBody] CreateNormFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateNormFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("NormFactor")]
    [OpenApiOperation("Update NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Update)]
    public async Task<IActionResult> UpdateNormFactor([FromBody] UpdateNormFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateNormFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("NormFactor")]
    [OpenApiOperation("Delete Many NormFactor", "")]
    [HasPermission(AppPermissions.Catalog.NormFactor.Delete)]
    public async Task<IActionResult> DeleteNormFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteNormFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SavingsRateConfig

    [HttpGet("SavingsRateConfig")]
    [OpenApiOperation("Get All SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Read)]
    public async Task<IActionResult> GetAllSavingsRateConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSavingsRateConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SavingsRateConfig/{id:guid}")]
    [OpenApiOperation("Get SavingsRateConfig By Id", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Read)]
    public async Task<IActionResult> GetSavingsRateConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetSavingsRateConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SavingsRateConfig/export")]
    [OpenApiOperation("Export SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Export)]
    public async Task<IActionResult> ExportSavingsRateConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelSavingsRateConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ty_le_tiet_kiem.xlsx");
        return result;
    }

    [HttpPost("SavingsRateConfig/import")]
    [OpenApiOperation("Import SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Import)]
    public async Task<IActionResult> ImportSavingsRateConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSavingsRateConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("SavingsRateConfig")]
    [OpenApiOperation("Create New SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Create)]
    public async Task<IActionResult> CreateSavingsRateConfig([FromBody] CreateSavingsRateConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateSavingsRateConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SavingsRateConfig")]
    [OpenApiOperation("Update SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Update)]
    public async Task<IActionResult> UpdateSavingsRateConfig([FromBody] SavingsRateConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateSavingsRateConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SavingsRateConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Delete)]
    public async Task<IActionResult> DeleteSavingsRateConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteSavingsRateConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SavingsRateConfig")]
    [OpenApiOperation("Delete Many SavingsRateConfig", "")]
    [HasPermission(AppPermissions.Catalog.SavingsRateConfig.Delete)]
    public async Task<IActionResult> DeleteSavingsRateConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteSavingsRateConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region AkFactorConfig

    [HttpGet("AkFactorConfig")]
    [OpenApiOperation("Get All AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Read)]
    public async Task<IActionResult> GetAllAkFactorConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAkFactorConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AkFactorConfig/{id:guid}")]
    [OpenApiOperation("Get AkFactorConfig By Id", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Read)]
    public async Task<IActionResult> GetAkFactorConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAkFactorConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AkFactorConfig/export")]
    [OpenApiOperation("Export AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Export)]
    public async Task<IActionResult> ExportAkFactorConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelAkFactorConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_Ak.xlsx");
        return result;
    }

    [HttpPost("AkFactorConfig/import")]
    [OpenApiOperation("Import AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Import)]
    public async Task<IActionResult> ImportAkFactorConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAkFactorConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("AkFactorConfig")]
    [OpenApiOperation("Create New AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Create)]
    public async Task<IActionResult> CreateAkFactorConfig([FromBody] CreateAkFactorConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateAkFactorConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AkFactorConfig")]
    [OpenApiOperation("Update AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Update)]
    public async Task<IActionResult> UpdateAkFactorConfig([FromBody] AkFactorConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAkFactorConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AkFactorConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Delete)]
    public async Task<IActionResult> DeleteAkFactorConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAkFactorConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AkFactorConfig")]
    [OpenApiOperation("Delete Many AkFactorConfig", "")]
    [HasPermission(AppPermissions.Catalog.AkFactorConfig.Delete)]
    public async Task<IActionResult> DeleteAkFactorConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAkFactorConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region RevenueCostAdjustmentConfig

    [HttpGet("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Get All RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Read)]
    public async Task<IActionResult> GetAllRevenueCostAdjustmentConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllRevenueCostAdjustmentConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("RevenueCostAdjustmentConfig/{id:guid}")]
    [OpenApiOperation("Get RevenueCostAdjustmentConfig By Id", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Read)]
    public async Task<IActionResult> GetRevenueCostAdjustmentConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetRevenueCostAdjustmentConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("RevenueCostAdjustmentConfig/export")]
    [OpenApiOperation("Export RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Export)]
    public async Task<IActionResult> ExportRevenueCostAdjustmentConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelRevenueCostAdjustmentConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ty_le_dieu_chinh_thu_chi.xlsx");
        return result;
    }

    [HttpPost("RevenueCostAdjustmentConfig/import")]
    [OpenApiOperation("Import RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Import)]
    public async Task<IActionResult> ImportRevenueCostAdjustmentConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportRevenueCostAdjustmentConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Create New RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Create)]
    public async Task<IActionResult> CreateRevenueCostAdjustmentConfig([FromBody] CreateRevenueCostAdjustmentConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateRevenueCostAdjustmentConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Update RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Update)]
    public async Task<IActionResult> UpdateRevenueCostAdjustmentConfig([FromBody] RevenueCostAdjustmentConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateRevenueCostAdjustmentConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("RevenueCostAdjustmentConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Delete)]
    public async Task<IActionResult> DeleteRevenueCostAdjustmentConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteRevenueCostAdjustmentConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Delete Many RevenueCostAdjustmentConfig", "")]
    [HasPermission(AppPermissions.Catalog.RevenueCostAdjustmentConfig.Delete)]
    public async Task<IActionResult> DeleteRevenueCostAdjustmentConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteRevenueCostAdjustmentConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region HaulDistance

    [HttpGet("HaulDistance")]
    [OpenApiOperation("Get All HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Read)]
    public async Task<IActionResult> GetAllHaulDistance([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<HaulDistance>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("HaulDistance/{id:guid}")]
    [OpenApiOperation("Get HaulDistance By Id", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Read)]
    public async Task<IActionResult> GetHaulDistanceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<HaulDistance>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("HaulDistance")]
    [OpenApiOperation("Create New HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Create)]
    public async Task<IActionResult> CreateHaulDistance([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<HaulDistance>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("HaulDistance")]
    [OpenApiOperation("Update HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Update)]
    public async Task<IActionResult> UpdateHaulDistance([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<HaulDistance>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("HaulDistance/{deleteId:guid}")]
    [OpenApiOperation("Delete HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Delete)]
    public async Task<IActionResult> DeleteHaulDistance([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<HaulDistance>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("HaulDistance")]
    [OpenApiOperation("Delete Many HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Delete)]
    public async Task<IActionResult> DeleteHaulDistanceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<HaulDistance>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("HaulDistance/export")]
    [OpenApiOperation("Export HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Export)]
    public async Task<IActionResult> ExportHaulDistance()
    {
        var fileByte = await Mediator.Send(new ExportExcelHaulDistanceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cung_do_van_tai.xlsx");
        return result;
    }

    [HttpPost("HaulDistance/import")]
    [OpenApiOperation("Import HaulDistance", "")]
    [HasPermission(AppPermissions.Catalog.HaulDistance.Import)]
    public async Task<IActionResult> ImportHaulDistance([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportHaulDistanceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region TransportLocation

    [HttpGet("TransportLocation")]
    [OpenApiOperation("Get All TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Read)]
    public async Task<IActionResult> GetAllTransportLocation([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTransportLocationQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TransportLocation/export")]
    [OpenApiOperation("Export TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Export)]
    public async Task<IActionResult> ExportTransportLocation()
    {
        var fileByte = await Mediator.Send(new ExportExcelTransportLocationQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vi_tri_nhan_do.xlsx");
        return result;
    }

    [HttpPost("TransportLocation/import")]
    [OpenApiOperation("Import TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Import)]
    public async Task<IActionResult> ImportTransportLocation([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTransportLocationExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TransportLocation/{id:guid}")]
    [OpenApiOperation("Get TransportLocation By Id", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Read)]
    public async Task<IActionResult> GetTransportLocationById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTransportLocationByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TransportLocation")]
    [OpenApiOperation("Create New TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Create)]
    public async Task<IActionResult> CreateTransportLocation([FromBody] CreateTransportLocationDto createModel)
    {
        var result = await Mediator.Send(new CreateTransportLocationCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("TransportLocation")]
    [OpenApiOperation("Update TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Update)]
    public async Task<IActionResult> UpdateTransportLocation([FromBody] UpdateTransportLocationDto updateModel)
    {
        var result = await Mediator.Send(new UpdateTransportLocationCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TransportLocation/{deleteId:guid}")]
    [OpenApiOperation("Delete TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Delete)]
    public async Task<IActionResult> DeleteTransportLocation([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTransportLocationCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TransportLocation")]
    [OpenApiOperation("Delete Many TransportLocation", "")]
    [HasPermission(AppPermissions.Catalog.TransportLocation.Delete)]
    public async Task<IActionResult> DeleteTransportLocationList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteTransportLocationListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region CargoType

    [HttpGet("CargoType")]
    [OpenApiOperation("Get All CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Read)]
    public async Task<IActionResult> GetAllCargoType([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllCargoTypeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("CargoType/export")]
    [OpenApiOperation("Export CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Export)]
    public async Task<IActionResult> ExportCargoType()
    {
        var fileByte = await Mediator.Send(new ExportExcelCargoTypeQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Loai_hang.xlsx");
        return result;
    }

    [HttpPost("CargoType/import")]
    [OpenApiOperation("Import CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Import)]
    public async Task<IActionResult> ImportCargoType([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportCargoTypeExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("CargoType/{Id:guid}")]
    [OpenApiOperation("Get CargoType By Id", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Read)]
    public async Task<IActionResult> GetCargoTypeById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetCargoTypeByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("CargoType")]
    [OpenApiOperation("Create New CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Create)]
    public async Task<IActionResult> CreateCargoType([FromBody] CreateCargoTypeDto createModel)
    {
        var result = await Mediator.Send(new CreateCargoTypeCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("CargoType")]
    [OpenApiOperation("Update CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Update)]
    public async Task<IActionResult> UpdateCargoType([FromBody] UpdateCargoTypeDto updateModel)
    {
        var result = await Mediator.Send(new UpdateCargoTypeCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("CargoType/{deleteId:guid}")]
    [OpenApiOperation("Delete CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Delete)]
    public async Task<IActionResult> DeleteCargoType([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteCargoTypeCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("CargoType")]
    [OpenApiOperation("Delete Many CargoType", "")]
    [HasPermission(AppPermissions.Catalog.CargoType.Delete)]
    public async Task<IActionResult> DeleteCargoTypeList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteCargoTypeListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion
}