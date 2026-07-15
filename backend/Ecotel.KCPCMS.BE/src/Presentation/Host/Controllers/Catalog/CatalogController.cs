using Application.Catalog.Index.AdjustmentFactor.Commands;
using Application.Catalog.Index.AdjustmentFactor.Queries;
using Application.Catalog.Index.AdjustmentFactorDescription.Commands;
using Application.Catalog.Index.AdjustmentFactorDescription.Queries;
using Application.Catalog.Index.AkFactorConfig.Commands;
using Application.Catalog.Index.AkFactorConfig.Queries;
using Application.Catalog.Index.AssignmentCodes.Commands;
using Application.Catalog.Index.AssignmentCodes.Queries;
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
using Application.Catalog.Index.UnitOfMeasures.Commands;
using Application.Catalog.Index.UnitOfMeasures.Queries;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.AkFactorConfig;
using Application.Dto.Catalog.AssignmentCode;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.Department;
using Application.Dto.Catalog.FixedKey;
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
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Host.Controllers.Base;
using Infrastructure.Auth.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class CatalogController : BaseAuthController
{
    #region UnitOfMeasure

    [HttpGet("UnitOfMeasure")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.read", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> GetAllUnitOfMeasure([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllUnitOfMeasureQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("UnitOfMeasure/{id:guid}")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.read", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> GetUnitOfMeasureById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("UnitOfMeasure")]
    [OpenApiOperation("Create New UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.create", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureDto createModel)
    {
        var result = await Mediator.Send(new CreateUnitOfMeasureCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("UnitOfMeasure/export")]
    [OpenApiOperation("Export UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.export", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> ExportUnitOfMeasure()
    {
        var fileByte = await Mediator.Send(new ExportExcelUnitOfMeasureQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "don_vi_tinh.xlsx");
        return result;
    }

    [HttpPost("UnitOfMeasure/import")]
    [OpenApiOperation("Import UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.import", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> ImportUnitOfMeasure([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportUnitOfMeasureExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPut("UnitOfMeasure")]
    [OpenApiOperation("Update UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.update", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> UpdateUnitOfMeasure([FromBody] UnitOfMeasureDto updateModel)
    {
        var result = await Mediator.Send(new UpdateUnitOfMeasureCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("UnitOfMeasure/{deleteId:guid}")]
    [OpenApiOperation("Delete UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.delete", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> DeleteUnitOfMeasure([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("UnitOfMeasure")]
    [OpenApiOperation("Delete Many UnitOfMeasure", "")]
    [HasPermission("catalog.unitofmeasure.delete", "Danh mục", "Đơn vị tính")]
    public async Task<IActionResult> DeleteUnitOfMeasureList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region AssignmentCode

    [HttpGet("AssignmentCode")]
    [OpenApiOperation("Get All AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.read", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> GetAllAssignmentCode([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAssignmentCodeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AssignmentCode/export")]
    [OpenApiOperation("Export AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.export", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> ExportAssignmentCode()
    {
        var fileByte = await Mediator.Send(new ExportExcelAssignmentCodeQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ma_giao_khoan.xlsx");
        return result;
    }

    [HttpPost("AssignmentCode/import")]
    [OpenApiOperation("Import AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.import", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> ImportAssignmentCode([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAssignmentCodeExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AssignmentCode/{id:guid}")]
    [OpenApiOperation("Get AssignmentCode by Id", "")]
    [HasPermission("catalog.assignmentcode.read", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> GetAssignmentCodeById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAssignmentCodeDetailByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AssignmentCode")]
    [OpenApiOperation("Create New AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.create", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> CreateAssignmentCode([FromBody] CreateAssignmentCodeDto createModel)
    {
        var result = await Mediator.Send(new CreateAssignmentCodeCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AssignmentCode")]
    [OpenApiOperation("Update AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.update", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateAssignmentCodeDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAssignmentCodeCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AssignmentCode/{deleteId:guid}")]
    [OpenApiOperation("Delete AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.delete", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> DeleteAssignmentCode([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AssignmentCode")]
    [OpenApiOperation("Delete Many AssignmentCode", "")]
    [HasPermission("catalog.assignmentcode.delete", "Danh mục", "Nhóm vật tư, tài sản")]
    public async Task<IActionResult> DeleteAssignmentCodeList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Department

    [HttpGet("Department")]
    [OpenApiOperation("Get All Department", "")]
    [HasPermission("catalog.department.read", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> GetAllDepartment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllDepartmentQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Department/export")]
    [OpenApiOperation("Export Department", "")]
    [HasPermission("catalog.department.export", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> ExportDepartment()
    {
        var fileByte = await Mediator.Send(new ExportExcelDepartmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Don_vi.xlsx");
        return result;
    }

    [HttpPost("Department/import")]
    [OpenApiOperation("Import Department", "")]
    [HasPermission("catalog.department.import", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> ImportDepartment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportDepartmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Department/{id:guid}")]
    [OpenApiOperation("Get Department By Id", "")]
    [HasPermission("catalog.department.read", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> GetDepartmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetDepartmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Department")]
    [OpenApiOperation("Create New Department", "")]
    [HasPermission("catalog.department.create", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto createModel)
    {
        var result = await Mediator.Send(new CreateDepartmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Department")]
    [OpenApiOperation("Update Department", "")]
    [HasPermission("catalog.department.update", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdateDepartmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Department/{deleteId:guid}")]
    [OpenApiOperation("Delete Department", "")]
    [HasPermission("catalog.department.delete", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> DeleteDepartment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteDepartmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Department")]
    [OpenApiOperation("Delete Many Department", "")]
    [HasPermission("catalog.department.delete", "Danh mục", "Đơn vị")]
    public async Task<IActionResult> DeleteDepartmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteDepartmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region Material

    [HttpGet("Material")]
    [OpenApiOperation("Get All Material", "")]
    [HasPermission("catalog.material.read", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> GetAllMaterial([FromQuery] MaterialType? materialType, [FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var normalizedMaterialType = materialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : materialType;
        var result = await Mediator.Send(new GetAllMaterialQuery(pageIndex, pageSize, search, ignorePagination, normalizedMaterialType, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Material/export")]
    [OpenApiOperation("Export Material", "")]
    [HasPermission("catalog.material.export", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> ExportMaterial([FromQuery] MaterialType materialType = MaterialType.MaterialInContract)
    {
        var normalizedMaterialType = materialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : materialType;
        var fileByte = await Mediator.Send(new ExportExcelMaterialQuery(normalizedMaterialType));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vat_tu_tai_san.xlsx");
        return result;
    }

    [HttpPost("Material/import")]
    [OpenApiOperation("Import Material", "")]
    [HasPermission("catalog.material.import", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> ImportMaterial([FromForm] ImportMaterialDto importModel)
    {
        var normalizedMaterialType = importModel.MaterialType == MaterialType.MaterialOutContract ? MaterialType.MaterialInContract : importModel.MaterialType;
        var result = await Mediator.Send(new ImportMaterialExcelCommand(importModel.FormFile, normalizedMaterialType));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Material/{id:guid}")]
    [OpenApiOperation("Get Material by Id", "")]
    [HasPermission("catalog.material.read", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> GetMaterialById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Material")]
    [OpenApiOperation("Create New Material", "")]
    [HasPermission("catalog.material.create", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialDto createModel)
    {
        var result = await Mediator.Send(new CreateMaterialCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Material")]
    [OpenApiOperation("Update AssignmentCode", "")]
    [HasPermission("catalog.material.update", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateMaterialDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMaterialCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Material/{deleteId:guid}")]
    [OpenApiOperation("Delete Material", "")]
    [HasPermission("catalog.material.delete", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> DeleteMaterial([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Material")]
    [OpenApiOperation("Delete Many Material", "")]
    [HasPermission("catalog.material.delete", "Danh mục", "Vật tư, tài sản")]
    public async Task<IActionResult> DeleteMaterialList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Position
    [HttpGet("Position")]
    [OpenApiOperation("Get All Position", "")]
    [HasPermission("catalog.position.read", "Danh mục", "Chức vụ")]
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
    [HasPermission("catalog.position.read", "Danh mục", "Chức vụ")]

    public async Task<IActionResult> GetPositionById([FromRoute] int id)
    {
        var result = await Mediator.Send(new GetPositionByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Position")]
    [OpenApiOperation("Create Position", "")]
    [HasPermission("catalog.position.create", "Danh mục", "Chức vụ")]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionDto dto)
    {
        var result = await Mediator.Send(new CreatePositionCommand(dto));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Position")]
    [OpenApiOperation("Update Position", "")]
    [HasPermission("catalog.position.update", "Danh mục", "Chức vụ")]
    public async Task<IActionResult> UpdatePosition([FromBody] UpdatePositionDto dto)
    {
        var result = await Mediator.Send(new UpdatePositionCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Position/{id:int}")]
    [OpenApiOperation("Delete Position", "")]
    [HasPermission("catalog.position.delete", "Danh mục", "Chức vụ")]
    public async Task<IActionResult> DeletePosition([FromRoute] int id)
    {
        var result = await Mediator.Send(new DeletePositionCommand(id));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Position/list")]
    [OpenApiOperation("Delete Position List", "")]
    [HasPermission("catalog.position.delete", "Danh mục", "Chức vụ")]

    public async Task<IActionResult> DeletePositionList([FromBody] IList<int> deleteIds)
    {
        var result = await Mediator.Send(new DeletePositionListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("Position/export")]
    [OpenApiOperation("Export Position List To Excel", "")]
    [HasPermission("catalog.position.export", "Danh mục", "Chức vụ")]

    public async Task<IActionResult> ExportPosition()
    {
        var fileBytes = await Mediator.Send(new ExportExcelPositionQuery());
        var fileName = $"danh-sach-chuc-vu-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("Position/import")]
    [Consumes("multipart/form-data")]
    [HasPermission("catalog.position.import", "Danh mục", "Chức vụ")]

    public async Task<IActionResult> ImportPosition([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportExcelPositionCommand(importModel.FormFile));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    #endregion


    #region ProcessGroup

    [HttpGet("ProcessGroup")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    [HasPermission("catalog.processgroup.read", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> GetAllProcessGroup([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProcessGroupQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProcessGroup/export")]
    [OpenApiOperation("Export ProcessGroup", "")]
    [HasPermission("catalog.processgroup.export", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> ExportProcessGroup()
    {
        var fileByte = await Mediator.Send(new ExportExcelProcessGroupQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Nhom_cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProcessGroup/import")]
    [OpenApiOperation("Import ProcessGroup", "")]
    [HasPermission("catalog.processgroup.import", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> ImportProcessGroup([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProcessGroupExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("ProcessGroup/{Id:guid}")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    [HasPermission("catalog.processgroup.read", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> GetProcessGroupById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProcessGroupByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("ProcessGroup")]
    [OpenApiOperation("Create New ProcessGroup", "")]
    [HasPermission("catalog.processgroup.create", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> CreateProcessGroup([FromBody] CreateProcessGroupDto createModel)
    {
        var result = await Mediator.Send(new CreateProcessGroupCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProcessGroup")]
    [OpenApiOperation("Update ProcessGroup", "")]
    [HasPermission("catalog.processgroup.update", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> UpdateProcessGroup([FromBody] UpdateProcessGroupDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProcessGroupCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProcessGroup/{deleteId:guid}")]
    [OpenApiOperation("Delete ProcessGroup", "")]
    [HasPermission("catalog.processgroup.delete", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> DeleteProcessGroup([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProcessGroupCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProcessGroup")]
    [OpenApiOperation("Delete Many ProcessGroup", "")]
    [HasPermission("catalog.processgroup.delete", "Danh mục", "Nhóm công đoạn sản xuất")]
    public async Task<IActionResult> DeleteProcessGroupList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProcessGroupListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductionProcess

    [HttpGet("ProductionProcess")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.read", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> GetAllProductionProcess([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionProcessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionProcess/export")]
    [OpenApiOperation("Export ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.export", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> ExportProductionProcess()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductionProcessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProductionProcess/import")]
    [OpenApiOperation("Import ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.import", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> ImportProductionProcess([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductionProcessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    // check api fe 
    [HttpPost("ProductionProcess")]
    [OpenApiOperation("Create New ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.create", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> CreateProductionProcess([FromBody] CreateProductionProcessDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionProcessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("ProductionProcess/{Id:guid}")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.read", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> GetProductionProcessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionProcessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ProductionProcess")]
    [OpenApiOperation("Update ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.update", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> UpdateProductionProcess([FromBody] UpdateProductionProcessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionProcessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionProcess/{deleteId:guid}")]
    [OpenApiOperation("Delete ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.delete", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> DeleteProductionProcess([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductionProcessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProductionProcess")]
    [OpenApiOperation("Delete Many ProductionProcess", "")]
    [HasPermission("catalog.productionprocess.delete", "Danh mục", "Công đoạn sản xuất")]
    public async Task<IActionResult> DeleteProductionProcessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionProcessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactor

    [HttpGet("AdjustmentFactor")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.read", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> GetAllAdjustmentFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/export")]
    [OpenApiOperation("Export AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.export", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> ExportAdjustmentFactor()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactor/import")]
    [OpenApiOperation("Import AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.import", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> ImportAdjustmentFactor([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactor/details")]
    [OpenApiOperation("Get All AdjustmentFactorDetail", "")]
    [HasPermission("catalog.adjustmentfactor.read", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> GetAllAdjustmentFactorDetail([FromQuery] Guid? ProcessGroupId)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDetailQuery(ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/{id:guid}")]
    [OpenApiOperation("Get AdjustmentFactor By Id", "")]
    [HasPermission("catalog.adjustmentfactor.read", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> GetAdjustmentFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactor")]
    [OpenApiOperation("Create New AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.create", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> CreateAdjustmentFactor([FromBody] CreateAdjustmentFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactor")]
    [OpenApiOperation("Update AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.update", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> UpdateAdjustmentFactor([FromBody] UpdateAdjustmentFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactor/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.delete", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> DeleteAdjustmentFactor([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactor")]
    [OpenApiOperation("Delete Many AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactor.delete", "Danh mục", "Hệ số điều chỉnh")]
    public async Task<IActionResult> DeleteAdjustmentFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactorDescription

    [HttpGet("AdjustmentFactorDescription")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    [HasPermission("catalog.adjustmentfactordescription.read", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> GetAllAdjustmentFactorDescription([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDescriptionQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/export")]
    [OpenApiOperation("Export AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.export", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> ExportAdjustmentFactorDescription()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorDescriptionQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dien_giai_he_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactorDescription/import")]
    [OpenApiOperation("Import AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.import", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> ImportAdjustmentFactorDescription([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorDescriptionExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/{id:guid}")]
    [OpenApiOperation("Get All AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.read", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> GetAdjustmentFactorDescriptionById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorDescriptionByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactorDescription")]
    [OpenApiOperation("Create New AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.create", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> CreateAdjustmentFactorDescription([FromBody] CreateAdjustmentFactorDescriptionDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorDescriptionCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactorDescription")]
    [OpenApiOperation("Update AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.update", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> UpdateAdjustmentFactorDescription([FromBody] UpdateAdjustmentFactorDescriptionDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorDescriptionCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.delete", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> DeleteAdjustmentFactorDescription([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription")]
    [OpenApiOperation("Delete Many AdjustmentFactorDescription", "")]
    [HasPermission("catalog.adjustmentfactordescription.delete", "Danh mục", "Diễn giải hệ số điều chỉnh")]
    public async Task<IActionResult> DeleteAdjustmentFactorDescriptionList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Passport

    [HttpGet("Passport")]
    [OpenApiOperation("Get All Passport", "")]
    [HasPermission("catalog.passport.read", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> GetAllPassport([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllPassportQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Passport/export")]
    [OpenApiOperation("Export Passport", "")]
    [HasPermission("catalog.passport.export", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> ExportPassport()
    {
        var fileByte = await Mediator.Send(new ExportExcelPassportQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ho_Chieu_Sd_Sc.xlsx");
        return result;
    }

    [HttpPost("Passport/import")]
    [OpenApiOperation("Import Passport", "")]
    [HasPermission("catalog.passport.import", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> ImportPassport([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPassportExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Passport/{id:guid}")]
    [OpenApiOperation("Get Passport By Id", "")]
    [HasPermission("catalog.passport.read", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> GetPassportById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPassportByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Passport")]
    [OpenApiOperation("Create New Passport", "")]
    [HasPermission("catalog.passport.create", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> CreatePassport([FromBody] CreatePassportDto createModel)
    {
        var result = await Mediator.Send(new CreatePassportCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Passport")]
    [OpenApiOperation("Update Passport", "")]
    [HasPermission("catalog.passport.update", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> UpdatePassport([FromBody] PassportDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePassportCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Passport/{deleteId:guid}")]
    [OpenApiOperation("Delete Passport", "")]
    [HasPermission("catalog.passport.delete", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> DeletePassport([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeletePassportCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Passport")]
    [OpenApiOperation("Delete Many Passport", "")]
    [HasPermission("catalog.passport.delete", "Danh mục", "Hộ chiếu, Sđ, Sc")]
    public async Task<IActionResult> DeletePassportList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePassportListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region LongwallParameters

    [HttpGet("LongwallParameters")]
    [OpenApiOperation("Get All LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.read", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> GetAllLongwallParameters([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallParametersQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("LongwallParameters/export")]
    [OpenApiOperation("Export LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.export", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> ExportLongwallParameters()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallParametersQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Thong_so_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallParameters/import")]
    [OpenApiOperation("Import LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.import", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> ImportLongwallParameters([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallParametersExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("LongwallParameters/{id:guid}")]
    [OpenApiOperation("Get LongwallParameters By Id", "")]
    [HasPermission("catalog.longwallparameters.read", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> GetLongwallParametersById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallParametersByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallParameters")]
    [OpenApiOperation("Create New LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.create", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> CreateLongwallParameters([FromBody] CreateLongwallParametersDto createModel)
    {
        var result = await Mediator.Send(new CreateLongwallParametersCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("LongwallParameters")]
    [OpenApiOperation("Update LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.update", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> UpdateLongwallParameters([FromBody] LongwallParametersDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallParametersCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallParameters/{deleteId:guid}")]
    [OpenApiOperation("Delete LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.delete", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> DeleteLongwallParameters([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallParameters")]
    [OpenApiOperation("Delete Many LongwallParameters", "")]
    [HasPermission("catalog.longwallparameters.delete", "Danh mục", "Thông số lò chợ")]
    public async Task<IActionResult> DeleteLongwallParametersList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region CuttingThickness

    [HttpGet("CuttingThickness")]
    [OpenApiOperation("Get All CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.read", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> GetAllCuttingThickness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllCuttingThicknessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("CuttingThickness/export")]
    [OpenApiOperation("Export CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.export", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> ExportCuttingThickness()
    {
        var fileByte = await Mediator.Send(new ExportExcelCuttingThicknessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chieu_day_lop_khau.xlsx");
        return result;
    }

    [HttpPost("CuttingThickness/import")]
    [OpenApiOperation("Import CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.import", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> ImportCuttingThickness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportCuttingThicknessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("CuttingThickness/{id:guid}")]
    [OpenApiOperation("Get CuttingThickness By Id", "")]
    [HasPermission("catalog.cuttingthickness.read", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> GetCuttingThicknessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetCuttingThicknessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("CuttingThickness")]
    [OpenApiOperation("Create New CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.create", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> CreateCuttingThicknesss([FromBody] CreateCuttingThicknessDto createModel)
    {
        var result = await Mediator.Send(new CreateCuttingThicknessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("CuttingThickness")]
    [OpenApiOperation("Update CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.update", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> UpdateCuttingThickness([FromBody] CuttingThicknessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateCuttingThicknessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("CuttingThickness/{deleteId:guid}")]
    [OpenApiOperation("Delete CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.delete", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> DeleteCuttingThickness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("CuttingThickness")]
    [OpenApiOperation("Delete Many CuttingThickness", "")]
    [HasPermission("catalog.cuttingthickness.delete", "Danh mục", "Chiều dày lớp khấu")]
    public async Task<IActionResult> DeleteCuttingThicknessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Hardness

    [HttpGet("Hardness")]
    [OpenApiOperation("Get All Hardness", "")]
    [HasPermission("catalog.hardness.read", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> GetAllHardness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Hardness>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Hardness/export")]
    [OpenApiOperation("Export Hardness", "")]
    [HasPermission("catalog.hardness.export", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> ExportHardness()
    {
        var fileByte = await Mediator.Send(new ExportExcelHardnessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Do_kien_co_than_da.xlsx");
        return result;
    }

    [HttpPost("Hardness/import")]
    [OpenApiOperation("Import Hardness", "")]
    [HasPermission("catalog.hardness.import", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> ImportHardness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportHardnessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Hardness/{Id:guid}")]
    [OpenApiOperation("Get Hardness By Id", "")]
    [HasPermission("catalog.hardness.read", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> GetHardnessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Hardness>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Hardness")]
    [OpenApiOperation("Create New Hardness", "")]
    [HasPermission("catalog.hardness.create", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> CreateHardness([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Hardness>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Hardness")]
    [OpenApiOperation("Update Hardness", "")]
    [HasPermission("catalog.hardness.update", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> UpdateHardness([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Hardness>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Hardness/{deleteId:guid}")]
    [OpenApiOperation("Delete Hardness", "")]
    [HasPermission("catalog.hardness.delete", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> DeleteHardness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Hardness>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Hardness")]
    [OpenApiOperation("Delete Many Hardness", "")]
    [HasPermission("catalog.hardness.delete", "Danh mục", "Độ kiên cố than đá")]
    public async Task<IActionResult> DeleteHardnessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Hardness>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Power

    [HttpGet("Power")]
    [OpenApiOperation("Get All Power", "")]
    [HasPermission("catalog.power.read", "Danh mục", "Công suất")]
    public async Task<IActionResult> GetAllPower([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Power>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Power/export")]
    [OpenApiOperation("Export Power", "")]
    [HasPermission("catalog.power.export", "Danh mục", "Công suất")]
    public async Task<IActionResult> ExportPower()
    {
        var fileByte = await Mediator.Send(new ExportExcelPowerQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_suat.xlsx");
        return result;
    }

    [HttpPost("Power/import")]
    [OpenApiOperation("Import Power", "")]
    [HasPermission("catalog.power.import", "Danh mục", "Công suất")]
    public async Task<IActionResult> ImportPower([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPowerExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Power/{Id:guid}")]
    [OpenApiOperation("Get Power By Id", "")]
    [HasPermission("catalog.power.read", "Danh mục", "Công suất")]
    public async Task<IActionResult> GetPowerById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Power>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Power")]
    [OpenApiOperation("Create New Power", "")]
    [HasPermission("catalog.power.create", "Danh mục", "Công suất")]
    public async Task<IActionResult> CreatePower([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Power>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Power")]
    [OpenApiOperation("Update Power", "")]
    [HasPermission("catalog.power.update", "Danh mục", "Công suất")]
    public async Task<IActionResult> UpdatePower([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Power>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Power/{deleteId:guid}")]
    [OpenApiOperation("Delete Power", "")]
    [HasPermission("catalog.power.delete", "Danh mục", "Công suất")]
    public async Task<IActionResult> DeletePower([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Power>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Power")]
    [OpenApiOperation("Delete Many Power", "")]
    [HasPermission("catalog.power.delete", "Danh mục", "Công suất")]
    public async Task<IActionResult> DeletePowerList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Power>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region InsertItem

    [HttpGet("InsertItem")]
    [OpenApiOperation("Get All InsertItem", "")]
    [HasPermission("catalog.insertitem.read", "Danh mục", "Chèn")]
    public async Task<IActionResult> GetAllInsertItem([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<InsertItem>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("InsertItem/export")]
    [OpenApiOperation("Export InsertItem", "")]
    [HasPermission("catalog.insertitem.export", "Danh mục", "Chèn")]
    public async Task<IActionResult> ExportInsertItem()
    {
        var fileByte = await Mediator.Send(new ExportExcelInsertItemQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chen.xlsx");
        return result;
    }

    [HttpPost("InsertItem/import")]
    [OpenApiOperation("Import InsertItem", "")]
    [HasPermission("catalog.insertitem.import", "Danh mục", "Chèn")]
    public async Task<IActionResult> ImportInsertItem([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportInsertItemExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("InsertItem/{id:guid}")]
    [OpenApiOperation("Get InsertItem By Id", "")]
    [HasPermission("catalog.insertitem.read", "Danh mục", "Chèn")]
    public async Task<IActionResult> GetInsertItemById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<InsertItem>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("InsertItem")]
    [OpenApiOperation("Create New InsertItem", "")]
    [HasPermission("catalog.insertitem.create", "Danh mục", "Chèn")]
    public async Task<IActionResult> CreateInsertItem([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<InsertItem>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("InsertItem")]
    [OpenApiOperation("Update InsertItem", "")]
    [HasPermission("catalog.insertitem.update", "Danh mục", "Chèn")]
    public async Task<IActionResult> UpdateInsertItem([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<InsertItem>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("InsertItem/{deleteId:guid}")]
    [OpenApiOperation("Delete InsertItem", "")]
    [HasPermission("catalog.insertitem.delete", "Danh mục", "Chèn")]
    public async Task<IActionResult> DeleteInsertItem([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<InsertItem>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("InsertItem")]
    [OpenApiOperation("Delete Many InsertItems", "")]
    [HasPermission("catalog.insertitem.delete", "Danh mục", "Chèn")]
    public async Task<IActionResult> DeleteInsertItemList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<InsertItem>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductionOrder

    [HttpGet("ProductionOrder")]
    [OpenApiOperation("Get All ProductionOrder", "")]
    [HasPermission("catalog.productionorder.read", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> GetAllProductionOrder([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionOrderQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOrder/{id:guid}")]
    [OpenApiOperation("Get ProductionOrder By Id", "")]
    [HasPermission("catalog.productionorder.read", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> GetProductionOrderById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionOrderByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOrder/export")]
    [OpenApiOperation("Export ProductionOrder", "")]
    [HasPermission("catalog.productionorder.export", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> ExportProductionOrder()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductionOrderQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Quyet_dinh_lenh_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProductionOrder/import")]
    [OpenApiOperation("Import ProductionOrder", "")]
    [HasPermission("catalog.productionorder.import", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> ImportProductionOrder([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductionOrderExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("ProductionOrder")]
    [OpenApiOperation("Create New ProductionOrder", "")]
    [HasPermission("catalog.productionorder.create", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> CreateProductionOrder([FromBody] CreateProductionOrderDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionOrderCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProductionOrder")]
    [OpenApiOperation("Update ProductionOrder", "")]
    [HasPermission("catalog.productionorder.update", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> UpdateProductionOrder([FromBody] ProductionOrderDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionOrderCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionOrder")]
    [OpenApiOperation("Delete Many ProductionOrder", "")]
    [HasPermission("catalog.productionorder.delete", "Danh mục", "Quyết định, lệnh sản xuất")]
    public async Task<IActionResult> DeleteProductionOrderList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionOrderListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Technology

    [HttpGet("Technology")]
    [OpenApiOperation("Get All Technology", "")]
    [HasPermission("catalog.technology.read", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> GetAllTechnology([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Technology>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Technology/export")]
    [OpenApiOperation("Export Technology", "")]
    [HasPermission("catalog.technology.export", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> ExportTechnology()
    {
        var fileByte = await Mediator.Send(new ExportExcelTechnologyQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_nghe.xlsx");
        return result;
    }

    [HttpPost("Technology/import")]
    [OpenApiOperation("Import Technology", "")]
    [HasPermission("catalog.technology.import", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> ImportTechnology([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTechnologyExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Technology/{id:guid}")]
    [OpenApiOperation("Get Technology By Id", "")]
    [HasPermission("catalog.technology.read", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> GetTechnologyById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Technology>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Technology")]
    [OpenApiOperation("Create New Technology", "")]
    [HasPermission("catalog.technology.create", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> CreateTechnology([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Technology>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Technology")]
    [OpenApiOperation("Update Technology", "")]
    [HasPermission("catalog.technology.update", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> UpdateTechnology([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Technology>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Technology/{deleteId:guid}")]
    [OpenApiOperation("Delete Technology", "")]
    [HasPermission("catalog.technology.delete", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> DeleteTechnology([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Technology>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Technology")]
    [OpenApiOperation("Delete Many Technology", "")]
    [HasPermission("catalog.technology.delete", "Danh mục", "Công nghệ khai thác")]
    public async Task<IActionResult> DeleteTechnologyList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Technology>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SeamFace

    [HttpGet("SeamFace")]
    [OpenApiOperation("Get All SeamFace", "")]
    [HasPermission("catalog.seamface.read", "Danh mục", "Mặt vỉa")]
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
    [HasPermission("catalog.seamface.export", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> ExportSeamFace()
    {
        var fileByte = await Mediator.Send(new ExportExcelSeamFaceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mat_nut.xlsx");
        return result;
    }

    [HttpPost("SeamFace/import")]
    [OpenApiOperation("Import SeamFace", "")]
    [HasPermission("catalog.seamface.import", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> ImportSeamFace([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSeamFaceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SeamFace/{id:guid}")]
    [OpenApiOperation("Get SeamFace By Id", "")]
    [HasPermission("catalog.seamface.read", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> GetSeamFaceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SeamFace>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SeamFace")]
    [OpenApiOperation("Create New SeamFace", "")]
    [HasPermission("catalog.seamface.create", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> CreateSeamFace([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SeamFace>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SeamFace")]
    [OpenApiOperation("Update SeamFace", "")]
    [HasPermission("catalog.seamface.update", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> UpdateSeamFace([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SeamFace>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SeamFace/{deleteId:guid}")]
    [OpenApiOperation("Delete SeamFace", "")]
    [HasPermission("catalog.seamface.delete", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> DeleteSeamFace([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SeamFace>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SeamFace")]
    [OpenApiOperation("Delete Many SeamFace", "")]
    [HasPermission("catalog.seamface.delete", "Danh mục", "Mặt vỉa")]
    public async Task<IActionResult> DeleteSeamFaceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SeamFace>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region StoneClampRatio

    [HttpGet("StoneClampRatio")]
    [OpenApiOperation("Get All StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.read", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> GetAllStoneClampRatio([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllStoneClampRatioQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/{id:guid}")]
    [OpenApiOperation("Get StoneClampRatio By Id", "")]
    [HasPermission("catalog.stoneclampratio.read", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> GetStoneClampRatioById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetStoneClampRatioByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/export")]
    [OpenApiOperation("Export StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.export", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> ExportStoneClampRatio()
    {
        var fileByte = await Mediator.Send(new ExportExcelStoneClampRatioQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ti_le_da_kep.xlsx");
        return result;
    }

    [HttpPost("StoneClampRatio/import")]
    [OpenApiOperation("Import StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.import", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> ImportStoneClampRatio([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportStoneClampRatioExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("StoneClampRatio")]
    [OpenApiOperation("Create New StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.create", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> CreateStoneClampRatio([FromBody] CreateStoneClampRatioDto createModel)
    {
        var result = await Mediator.Send(new CreateStoneClampRatioCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("StoneClampRatio")]
    [OpenApiOperation("Update StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.update", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> UpdateStoneClampRatio([FromBody] UpdateStoneClampRatioDto updateModel)
    {
        var result = await Mediator.Send(new UpdateStoneClampRatioCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("StoneClampRatio/{deleteId:guid}")]
    [OpenApiOperation("Delete StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.delete", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> DeleteStoneClampRatio([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("StoneClampRatio")]
    [OpenApiOperation("Delete Many StoneClampRatio", "")]
    [HasPermission("catalog.stoneclampratio.delete", "Danh mục", "Tỉ lệ đá kẹp")]
    public async Task<IActionResult> DeleteStoneClampRatioList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SupportStep

    [HttpGet("SupportStep")]
    [OpenApiOperation("Get All SupportStep", "")]
    [HasPermission("catalog.supportstep.read", "Danh mục", "Bước chống")]
    public async Task<IActionResult> GetAllSupportStep([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<SupportStep>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SupportStep/export")]
    [OpenApiOperation("Export SupportStep", "")]
    [HasPermission("catalog.supportstep.export", "Danh mục", "Bước chống")]
    public async Task<IActionResult> ExportSupportStep()
    {
        var fileByte = await Mediator.Send(new ExportExcelSupportStepQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Buoc_chong.xlsx");
        return result;
    }

    [HttpPost("SupportStep/import")]
    [OpenApiOperation("Import SupportStep", "")]
    [HasPermission("catalog.supportstep.import", "Danh mục", "Bước chống")]
    public async Task<IActionResult> ImportSupportStep([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSupportStepExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SupportStep/{id:guid}")]
    [OpenApiOperation("Get SupportStep By Id", "")]
    [HasPermission("catalog.supportstep.read", "Danh mục", "Bước chống")]
    public async Task<IActionResult> GetSupportStepById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SupportStep>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SupportStep")]
    [OpenApiOperation("Create New SupportStep", "")]
    [HasPermission("catalog.supportstep.create", "Danh mục", "Bước chống")]
    public async Task<IActionResult> CreateSupportStep([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SupportStep>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SupportStep")]
    [OpenApiOperation("Update SupportStep", "")]
    [HasPermission("catalog.supportstep.update", "Danh mục", "Bước chống")]
    public async Task<IActionResult> UpdateSupportStep([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SupportStep>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SupportStep/{deleteId:guid}")]
    [OpenApiOperation("Delete SupportStep", "")]
    [HasPermission("catalog.supportstep.delete", "Danh mục", "Bước chống")]
    public async Task<IActionResult> DeleteSupportStep([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SupportStep>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SupportStep")]
    [OpenApiOperation("Delete Many SupportStep", "")]
    [HasPermission("catalog.supportstep.delete", "Danh mục", "Bước chống")]
    public async Task<IActionResult> DeleteSupportStepList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SupportStep>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Product

    [HttpGet("Product")]
    [OpenApiOperation("Get All Product", "")]
    [HasPermission("catalog.product.read", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> GetAllProduct([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Product/export")]
    [OpenApiOperation("Export Product", "")]
    [HasPermission("catalog.product.export", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> ExportProduct()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "San_pham.xlsx");
        return result;
    }

    [HttpPost("Product/import")]
    [OpenApiOperation("Import Product", "")]
    [HasPermission("catalog.product.import", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> ImportProduct([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Product/{id:guid}")]
    [OpenApiOperation("Get Product By Id", "")]
    [HasPermission("catalog.product.read", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Product")]
    [OpenApiOperation("Create New Product", "")]
    [HasPermission("catalog.product.create", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createModel)
    {
        var result = await Mediator.Send(new CreateProductCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Product")]
    [OpenApiOperation("Update Product", "")]
    [HasPermission("catalog.product.update", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Product/{deleteId:guid}")]
    [OpenApiOperation("Delete Product", "")]
    [HasPermission("catalog.product.delete", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Product")]
    [OpenApiOperation("Delete Many Product", "")]
    [HasPermission("catalog.product.delete", "Danh mục", "Sản phẩm")]
    public async Task<IActionResult> DeleteProductList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region NormFactor

    [HttpGet("NormFactor")]
    [OpenApiOperation("Get All NormFactor", "")]
    [HasPermission("catalog.normfactor.read", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> GetAllNormFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllNormFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("NormFactor/{id:guid}")]
    [OpenApiOperation("Get NormFactor By Id", "")]
    [HasPermission("catalog.normfactor.read", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> GetNormFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetNormFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("NormFactor/export")]
    [OpenApiOperation("Export NormFactor", "")]
    [HasPermission("catalog.normfactor.export", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> ExportNormFactor()
    {
        var fileByte = await Mediator.Send(new ExportExcelNormFactorQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("NormFactor/import")]
    [OpenApiOperation("Import NormFactor", "")]
    [HasPermission("catalog.normfactor.import", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> ImportNormFactor([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportNormFactorExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("NormFactor")]
    [OpenApiOperation("Create New NormFactor", "")]
    [HasPermission("catalog.normfactor.create", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> CreateNormFactor([FromBody] CreateNormFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateNormFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("NormFactor")]
    [OpenApiOperation("Update NormFactor", "")]
    [HasPermission("catalog.normfactor.update", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> UpdateNormFactor([FromBody] UpdateNormFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateNormFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("NormFactor")]
    [OpenApiOperation("Delete Many NormFactor", "")]
    [HasPermission("catalog.normfactor.delete", "Danh mục", "Hệ số định mức")]
    public async Task<IActionResult> DeleteNormFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteNormFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SavingsRateConfig

    [HttpGet("SavingsRateConfig")]
    [OpenApiOperation("Get All SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.read", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> GetAllSavingsRateConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSavingsRateConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SavingsRateConfig/{id:guid}")]
    [OpenApiOperation("Get SavingsRateConfig By Id", "")]
    [HasPermission("catalog.savingsrateconfig.read", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> GetSavingsRateConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetSavingsRateConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SavingsRateConfig/export")]
    [OpenApiOperation("Export SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.export", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> ExportSavingsRateConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelSavingsRateConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ty_le_tiet_kiem.xlsx");
        return result;
    }

    [HttpPost("SavingsRateConfig/import")]
    [OpenApiOperation("Import SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.import", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> ImportSavingsRateConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSavingsRateConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("SavingsRateConfig")]
    [OpenApiOperation("Create New SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.create", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> CreateSavingsRateConfig([FromBody] CreateSavingsRateConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateSavingsRateConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SavingsRateConfig")]
    [OpenApiOperation("Update SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.update", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> UpdateSavingsRateConfig([FromBody] SavingsRateConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateSavingsRateConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SavingsRateConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.delete", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> DeleteSavingsRateConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteSavingsRateConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SavingsRateConfig")]
    [OpenApiOperation("Delete Many SavingsRateConfig", "")]
    [HasPermission("catalog.savingsrateconfig.delete", "Danh mục", "Hệ số tiết kiệm")]
    public async Task<IActionResult> DeleteSavingsRateConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteSavingsRateConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region AkFactorConfig

    [HttpGet("AkFactorConfig")]
    [OpenApiOperation("Get All AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.read", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> GetAllAkFactorConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAkFactorConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AkFactorConfig/{id:guid}")]
    [OpenApiOperation("Get AkFactorConfig By Id", "")]
    [HasPermission("catalog.akfactorconfig.read", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> GetAkFactorConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAkFactorConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AkFactorConfig/export")]
    [OpenApiOperation("Export AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.export", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> ExportAkFactorConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelAkFactorConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_Ak.xlsx");
        return result;
    }

    [HttpPost("AkFactorConfig/import")]
    [OpenApiOperation("Import AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.import", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> ImportAkFactorConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAkFactorConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("AkFactorConfig")]
    [OpenApiOperation("Create New AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.create", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> CreateAkFactorConfig([FromBody] CreateAkFactorConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateAkFactorConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AkFactorConfig")]
    [OpenApiOperation("Update AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.update", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> UpdateAkFactorConfig([FromBody] AkFactorConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAkFactorConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AkFactorConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.delete", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> DeleteAkFactorConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAkFactorConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AkFactorConfig")]
    [OpenApiOperation("Delete Many AkFactorConfig", "")]
    [HasPermission("catalog.akfactorconfig.delete", "Danh mục", "Hệ số Ak")]
    public async Task<IActionResult> DeleteAkFactorConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAkFactorConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region RevenueCostAdjustmentConfig

    [HttpGet("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Get All RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.read", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> GetAllRevenueCostAdjustmentConfig([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllRevenueCostAdjustmentConfigQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("RevenueCostAdjustmentConfig/{id:guid}")]
    [OpenApiOperation("Get RevenueCostAdjustmentConfig By Id", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.read", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> GetRevenueCostAdjustmentConfigById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetRevenueCostAdjustmentConfigByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("RevenueCostAdjustmentConfig/export")]
    [OpenApiOperation("Export RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.export", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> ExportRevenueCostAdjustmentConfig()
    {
        var fileByte = await Mediator.Send(new ExportExcelRevenueCostAdjustmentConfigQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ty_le_dieu_chinh_thu_chi.xlsx");
        return result;
    }

    [HttpPost("RevenueCostAdjustmentConfig/import")]
    [OpenApiOperation("Import RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.import", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> ImportRevenueCostAdjustmentConfig([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportRevenueCostAdjustmentConfigExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Create New RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.create", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> CreateRevenueCostAdjustmentConfig([FromBody] CreateRevenueCostAdjustmentConfigDto createModel)
    {
        var result = await Mediator.Send(new CreateRevenueCostAdjustmentConfigCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Update RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.update", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> UpdateRevenueCostAdjustmentConfig([FromBody] RevenueCostAdjustmentConfigDto updateModel)
    {
        var result = await Mediator.Send(new UpdateRevenueCostAdjustmentConfigCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("RevenueCostAdjustmentConfig/{deleteId:guid}")]
    [OpenApiOperation("Delete RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.delete", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> DeleteRevenueCostAdjustmentConfig([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteRevenueCostAdjustmentConfigCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("RevenueCostAdjustmentConfig")]
    [OpenApiOperation("Delete Many RevenueCostAdjustmentConfig", "")]
    [HasPermission("catalog.revenuecostadjustmentconfig.delete", "Danh mục", "Giá trị tiết kiệm")]
    public async Task<IActionResult> DeleteRevenueCostAdjustmentConfigList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteRevenueCostAdjustmentConfigListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion
}