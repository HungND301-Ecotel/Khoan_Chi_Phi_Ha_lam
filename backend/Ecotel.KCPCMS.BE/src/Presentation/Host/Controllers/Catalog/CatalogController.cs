using Application.Catalog.Index.AdjustmentFactor.Commands;
using Application.Catalog.Index.AdjustmentFactor.Queries;
using Application.Catalog.Index.AdjustmentFactorDescription.Commands;
using Application.Catalog.Index.AdjustmentFactorDescription.Queries;
using Application.Catalog.Index.AssignmentCodes.Commands;
using Application.Catalog.Index.AssignmentCodes.Queries;
using Application.Catalog.Index.CuttingThickness.Commands;
using Application.Catalog.Index.CuttingThickness.Queries;
using Application.Catalog.Index.Equipments.Commands;
using Application.Catalog.Index.Equipments.Queries;
using Application.Catalog.Index.LongwallParameters.Commands;
using Application.Catalog.Index.LongwallParameters.Queries;
using Application.Catalog.Index.Material.Commands;
using Application.Catalog.Index.Material.Queries;
using Application.Catalog.Index.Metrics.Commands;
using Application.Catalog.Index.Metrics.Queries;
using Application.Catalog.Index.Part.Commands.Part;
using Application.Catalog.Index.Part.Queries.Part;
using Application.Catalog.Index.Passport.Commands;
using Application.Catalog.Index.Passport.Queries;
using Application.Catalog.Index.ProcessGroups.Commands;
using Application.Catalog.Index.ProcessGroups.Queries;
using Application.Catalog.Index.Product.Commands;
using Application.Catalog.Index.Product.Queries;
using Application.Catalog.Index.ProductionOrder.Commands;
using Application.Catalog.Index.ProductionProcess.Commands;
using Application.Catalog.Index.ProductionProcess.Queries;
using Application.Catalog.Index.StoneClampRatio.Commands;
using Application.Catalog.Index.StoneClampRatio.Queries;
using Application.Catalog.Index.UnitOfMeasures.Commands;
using Application.Catalog.Index.UnitOfMeasures.Queries;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.AssignmentCode;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.Equipment;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.Material;
using Application.Dto.Catalog.Metric;
using Application.Dto.Catalog.NormFactor;
using Application.Dto.Catalog.Part;
using Application.Dto.Catalog.Passport;
using Application.Dto.Catalog.ProcessGroup;
using Application.Dto.Catalog.Product;
using Application.Dto.Catalog.ProductionOrder;
using Application.Dto.Catalog.ProductionProcess;
using Application.Dto.Catalog.StoneClampRatio;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class CatalogController : BaseNoAuthController
{
    #region UnitOfMeasure

    [HttpGet("UnitOfMeasure")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    public async Task<IActionResult> GetAllUnitOfMeasure([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllUnitOfMeasureQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("UnitOfMeasure/{id:guid}")]
    [OpenApiOperation("Get All UnitOfMeasure", "")]
    public async Task<IActionResult> GetUnitOfMeasureById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("UnitOfMeasure")]
    [OpenApiOperation("Create New UnitOfMeasure", "")]
    public async Task<IActionResult> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureDto createModel)
    {
        var result = await Mediator.Send(new CreateUnitOfMeasureCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("UnitOfMeasure/export")]
    [OpenApiOperation("Export UnitOfMeasure", "")]
    public async Task<IActionResult> ExportUnitOfMeasure()
    {
        var fileByte = await Mediator.Send(new ExportExcelUnitOfMeasureQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "don_vi_tinh.xlsx");
        return result;
    }

    [HttpPost("UnitOfMeasure/import")]
    [OpenApiOperation("Import UnitOfMeasure", "")]
    public async Task<IActionResult> ImportUnitOfMeasure([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportUnitOfMeasureExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPut("UnitOfMeasure")]
    [OpenApiOperation("Update UnitOfMeasure", "")]
    public async Task<IActionResult> UpdateUnitOfMeasure([FromBody] UnitOfMeasureDto updateModel)
    {
        var result = await Mediator.Send(new UpdateUnitOfMeasureCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("UnitOfMeasure/{deleteId:guid}")]
    [OpenApiOperation("Delete UnitOfMeasure", "")]
    public async Task<IActionResult> DeleteUnitOfMeasure([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("UnitOfMeasure")]
    [OpenApiOperation("Delete Many UnitOfMeasure", "")]
    public async Task<IActionResult> DeleteUnitOfMeasureList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteUnitOfMeasureListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region AssignmentCode

    [HttpGet("AssignmentCode")]
    [OpenApiOperation("Get All AssignmentCode", "")]
    public async Task<IActionResult> GetAllAssignmentCode([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAssignmentCodeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AssignmentCode/export")]
    [OpenApiOperation("Export AssignmentCode", "")]
    public async Task<IActionResult> ExportAssignmentCode()
    {
        var fileByte = await Mediator.Send(new ExportExcelAssignmentCodeQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ma_giao_khoan.xlsx");
        return result;
    }

    [HttpPost("AssignmentCode/import")]
    [OpenApiOperation("Import AssignmentCode", "")]
    public async Task<IActionResult> ImportAssignmentCode([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAssignmentCodeExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AssignmentCode/{id:guid}")]
    [OpenApiOperation("Get AssignmentCode by Id", "")]
    public async Task<IActionResult> GetAssignmentCodeById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAssignmentCodeDetailByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AssignmentCode")]
    [OpenApiOperation("Create New AssignmentCode", "")]
    public async Task<IActionResult> CreateAssignmentCode([FromBody] CreateAssignmentCodeDto createModel)
    {
        var result = await Mediator.Send(new CreateAssignmentCodeCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AssignmentCode")]
    [OpenApiOperation("Update AssignmentCode", "")]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateAssignmentCodeDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAssignmentCodeCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AssignmentCode/{deleteId:guid}")]
    [OpenApiOperation("Delete AssignmentCode", "")]
    public async Task<IActionResult> DeleteAssignmentCode([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AssignmentCode")]
    [OpenApiOperation("Delete Many AssignmentCode", "")]
    public async Task<IActionResult> DeleteAssignmentCodeList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAssignmentCodeListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Material

    [HttpGet("Material")]
    [OpenApiOperation("Get All Material", "")]
    public async Task<IActionResult> GetAllMaterial([FromQuery] MaterialType? materialType, [FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaterialQuery(pageIndex, pageSize, search, ignorePagination, materialType, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Material/export")]
    [OpenApiOperation("Export Material", "")]
    public async Task<IActionResult> ExportMaterial()
    {
        var fileByte = await Mediator.Send(new ExportExcelMaterialQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vat_tu_tai_san.xlsx");
        return result;
    }

    [HttpPost("Material/import")]
    [OpenApiOperation("Import Material", "")]
    public async Task<IActionResult> ImportMaterial([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportMaterialExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Material/{id:guid}")]
    [OpenApiOperation("Get Material by Id", "")]
    public async Task<IActionResult> GetMaterialById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Material")]
    [OpenApiOperation("Create New Material", "")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialDto createModel)
    {
        var result = await Mediator.Send(new CreateMaterialCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Material")]
    [OpenApiOperation("Update AssignmentCode", "")]
    public async Task<IActionResult> UpdateAssignmentCode([FromBody] UpdateMaterialDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMaterialCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Material/{deleteId:guid}")]
    [OpenApiOperation("Delete Material", "")]
    public async Task<IActionResult> DeleteMaterial([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Material")]
    [OpenApiOperation("Delete Many Material", "")]
    public async Task<IActionResult> DeleteMaterialList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Equiqment

    [HttpGet("Equipment")]
    [OpenApiOperation("Get All Equipment", "")]
    public async Task<IActionResult> GetAllEquipment([FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllEquipmentQuery(pageIndex, pageSize, search, ignorePagination, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Equipment/export")]
    [OpenApiOperation("Export Equipment", "")]
    public async Task<IActionResult> ExportEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Thiet_bi.xlsx");
        return result;
    }

    [HttpPost("Equipment/import")]
    [OpenApiOperation("Import Equipment", "")]
    public async Task<IActionResult> ImportEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Equipment/{id:guid}")]
    [OpenApiOperation("Get Equipment by Id", "")]
    public async Task<IActionResult> GetEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Equipment/{id:guid}/Parts")]
    [OpenApiOperation("Get Equipment Parts by equipmentId", "")]
    public async Task<IActionResult> GetEquipmentPartsById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAllPartByEquipmentIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Equipment")]
    [OpenApiOperation("Create New Equipment", "")]
    public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentDto createModel)
    {
        var result = await Mediator.Send(new CreateEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Equipment")]
    [OpenApiOperation("Update Equipment", "")]
    public async Task<IActionResult> UpdateEquipment([FromBody] UpdateEquipmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdateEquipmentsCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Equipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Equipment", "")]
    public async Task<IActionResult> DeleteEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Equipment")]
    [OpenApiOperation("Delete Many Equipment", "")]
    public async Task<IActionResult> DeleteEquipmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Part

    [HttpGet("Part")]
    [OpenApiOperation("Get All Part", "")]
    public async Task<IActionResult> GetAllPart([FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllPartQuery(pageIndex, pageSize, search, ignorePagination, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Part/export")]
    [OpenApiOperation("Export Part", "")]
    public async Task<IActionResult> ExportPart()
    {
        var fileByte = await Mediator.Send(new ExportExcelPartQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Phu_tung.xlsx");
        return result;
    }

    [HttpPost("Part/import")]
    [OpenApiOperation("Import Part", "")]
    public async Task<IActionResult> ImportPart([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPartExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Part/{id:guid}")]
    [OpenApiOperation("Get Part by Id", "")]
    public async Task<IActionResult> GetPartById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPartByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Part")]
    [OpenApiOperation("Create New Part", "")]
    public async Task<IActionResult> CreatePart([FromBody] CreatePartDto createModel)
    {
        var result = await Mediator.Send(new CreatePartCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Part")]
    [OpenApiOperation("Update Part", "")]
    public async Task<IActionResult> UpdatePart([FromBody] UpdatePartDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePartCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Part/{deleteId:guid}")]
    [OpenApiOperation("Delete Part", "")]
    public async Task<IActionResult> DeletePart([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeletePartCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Part")]
    [OpenApiOperation("Delete Many Part", "")]
    public async Task<IActionResult> DeletePartList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePartListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region OtherPart
    [HttpGet("OtherPart")]
    [OpenApiOperation("Get All OtherPart", "")]
    public async Task<IActionResult> GetAllOtherPart([FromQuery] DateTime? date, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllOtherPartQuery(pageIndex, pageSize, search, ignorePagination, date ?? DateTime.UtcNow));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("OtherPart/{id:guid}")]
    [OpenApiOperation("Get OtherPart by Id", "")]
    public async Task<IActionResult> GetOtherPartById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetOtherPartByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("OtherPart")]
    [OpenApiOperation("Create New OtherPart", "")]
    public async Task<IActionResult> CreateOtherPart([FromBody] CreateOtherPartDto createModel)
    {
        var result = await Mediator.Send(new CreateOtherPartCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("OtherPart")]
    [OpenApiOperation("Update OtherPart", "")]
    public async Task<IActionResult> UpdateOtherPart([FromBody] UpdateOtherPartDto updateModel)
    {
        var result = await Mediator.Send(new UpdateOtherPartCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("OtherPart")]
    [OpenApiOperation("Delete Many OtherPart", "")]
    public async Task<IActionResult> DeleteOtherPartList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteOtherPartListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProcessGroup

    [HttpGet("ProcessGroup")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    public async Task<IActionResult> GetAllProcessGroup([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProcessGroupQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProcessGroup/export")]
    [OpenApiOperation("Export ProcessGroup", "")]
    public async Task<IActionResult> ExportProcessGroup()
    {
        var fileByte = await Mediator.Send(new ExportExcelProcessGroupQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Nhom_cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProcessGroup/import")]
    [OpenApiOperation("Import ProcessGroup", "")]
    public async Task<IActionResult> ImportProcessGroup([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProcessGroupExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("ProcessGroup/{Id:guid}")]
    [OpenApiOperation("Get All ProcessGroup", "")]
    public async Task<IActionResult> GetProcessGroupById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProcessGroupByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("ProcessGroup")]
    [OpenApiOperation("Create New ProcessGroup", "")]
    public async Task<IActionResult> CreateProcessGroup([FromBody] CreateProcessGroupDto createModel)
    {
        var result = await Mediator.Send(new CreateProcessGroupCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProcessGroup")]
    [OpenApiOperation("Update ProcessGroup", "")]
    public async Task<IActionResult> UpdateProcessGroup([FromBody] ProcessGroupDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProcessGroupCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProcessGroup/{deleteId:guid}")]
    [OpenApiOperation("Delete ProcessGroup", "")]
    public async Task<IActionResult> DeleteProcessGroup([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProcessGroupCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProcessGroup")]
    [OpenApiOperation("Delete Many ProcessGroup", "")]
    public async Task<IActionResult> DeleteProcessGroupList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProcessGroupListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductionProcess

    [HttpGet("ProductionProcess")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    public async Task<IActionResult> GetAllProductionProcess([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionProcessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionProcess/export")]
    [OpenApiOperation("Export ProductionProcess", "")]
    public async Task<IActionResult> ExportProductionProcess()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductionProcessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_doan_san_xuat.xlsx");
        return result;
    }

    [HttpPost("ProductionProcess/import")]
    [OpenApiOperation("Import ProductionProcess", "")]
    public async Task<IActionResult> ImportProductionProcess([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductionProcessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("ProductionProcess")]
    [OpenApiOperation("Create New ProductionProcess", "")]
    public async Task<IActionResult> CreateProcessGroup([FromBody] CreateProductionProcessDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionProcessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("ProductionProcess/{Id:guid}")]
    [OpenApiOperation("Get All ProductionProcess", "")]
    public async Task<IActionResult> GetProductionProcessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionProcessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ProductionProcess")]
    [OpenApiOperation("Update ProductionProcess", "")]
    public async Task<IActionResult> UpdateProductionProcess([FromBody] UpdateProductionProcessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionProcessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionProcess/{deleteId:guid}")]
    [OpenApiOperation("Delete ProductionProcess", "")]
    public async Task<IActionResult> DeleteProductionProcess([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductionProcessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("ProductionProcess")]
    [OpenApiOperation("Delete Many ProductionProcess", "")]
    public async Task<IActionResult> DeleteProductionProcessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionProcessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactor

    [HttpGet("AdjustmentFactor")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    public async Task<IActionResult> GetAllAdjustmentFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/export")]
    [OpenApiOperation("Export AdjustmentFactor", "")]
    public async Task<IActionResult> ExportAdjustmentFactor()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "He_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactor/import")]
    [OpenApiOperation("Import AdjustmentFactor", "")]
    public async Task<IActionResult> ImportAdjustmentFactor([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactor/details")]
    [OpenApiOperation("Get All AdjustmentFactorDetail", "")]
    public async Task<IActionResult> GetAllAdjustmentFactorDetail([FromQuery] Guid? ProcessGroupId)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDetailQuery(ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactor/{id:guid}")]
    [OpenApiOperation("Get AdjustmentFactor By Id", "")]
    public async Task<IActionResult> GetAdjustmentFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactor")]
    [OpenApiOperation("Create New AdjustmentFactor", "")]
    public async Task<IActionResult> CreateAdjustmentFactor([FromBody] CreateAdjustmentFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactor")]
    [OpenApiOperation("Update AdjustmentFactor", "")]
    public async Task<IActionResult> UpdateAdjustmentFactor([FromBody] UpdateAdjustmentFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactor/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactor", "")]
    public async Task<IActionResult> DeleteAdjustmentFactor([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactor")]
    [OpenApiOperation("Delete Many AdjustmentFactor", "")]
    public async Task<IActionResult> DeleteAdjustmentFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region AdjustmentFactorDescription

    [HttpGet("AdjustmentFactorDescription")]
    [OpenApiOperation("Get All AdjustmentFactor", "")]
    public async Task<IActionResult> GetAllAdjustmentFactorDescription([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllAdjustmentFactorDescriptionQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/export")]
    [OpenApiOperation("Export AdjustmentFactorDescription", "")]
    public async Task<IActionResult> ExportAdjustmentFactorDescription()
    {
        var fileByte = await Mediator.Send(new ExportExcelAdjustmentFactorDescriptionQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dien_giai_he_so_dieu_chinh.xlsx");
        return result;
    }

    [HttpPost("AdjustmentFactorDescription/import")]
    [OpenApiOperation("Import AdjustmentFactorDescription", "")]
    public async Task<IActionResult> ImportAdjustmentFactorDescription([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportAdjustmentFactorDescriptionExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("AdjustmentFactorDescription/{id:guid}")]
    [OpenApiOperation("Get All AdjustmentFactorDescription", "")]
    public async Task<IActionResult> GetAdjustmentFactorDescriptionById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentFactorDescriptionByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("AdjustmentFactorDescription")]
    [OpenApiOperation("Create New AdjustmentFactorDescription", "")]
    public async Task<IActionResult> CreateAdjustmentFactorDescription([FromBody] CreateAdjustmentFactorDescriptionDto createModel)
    {
        var result = await Mediator.Send(new CreateAdjustmentFactorDescriptionCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("AdjustmentFactorDescription")]
    [OpenApiOperation("Update AdjustmentFactorDescription", "")]
    public async Task<IActionResult> UpdateAdjustmentFactorDescription([FromBody] UpdateAdjustmentFactorDescriptionDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentFactorDescriptionCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription/{deleteId:guid}")]
    [OpenApiOperation("Delete AdjustmentFactorDescription", "")]
    public async Task<IActionResult> DeleteAdjustmentFactorDescription([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("AdjustmentFactorDescription")]
    [OpenApiOperation("Delete Many AdjustmentFactorDescription", "")]
    public async Task<IActionResult> DeleteAdjustmentFactorDescriptionList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteAdjustmentFactorDescriptionListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Passport

    [HttpGet("Passport")]
    [OpenApiOperation("Get All Passport", "")]
    public async Task<IActionResult> GetAllPassport([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllPassportQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Passport/export")]
    [OpenApiOperation("Export Passport", "")]
    public async Task<IActionResult> ExportPassport()
    {
        var fileByte = await Mediator.Send(new ExportExcelPassportQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ho_Chieu_Sd_Sc.xlsx");
        return result;
    }

    [HttpPost("Passport/import")]
    [OpenApiOperation("Import Passport", "")]
    public async Task<IActionResult> ImportPassport([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportPassportExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Passport/{id:guid}")]
    [OpenApiOperation("Get Passport By Id", "")]
    public async Task<IActionResult> GetPassportById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPassportByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Passport")]
    [OpenApiOperation("Create New Passport", "")]
    public async Task<IActionResult> CreatePassport([FromBody] CreatePassportDto createModel)
    {
        var result = await Mediator.Send(new CreatePassportCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Passport")]
    [OpenApiOperation("Update Passport", "")]
    public async Task<IActionResult> UpdatePassport([FromBody] PassportDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePassportCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Passport/{deleteId:guid}")]
    [OpenApiOperation("Delete Passport", "")]
    public async Task<IActionResult> DeletePassport([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeletePassportCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Passport")]
    [OpenApiOperation("Delete Many Passport", "")]
    public async Task<IActionResult> DeletePassportList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePassportListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region LongwallParameters

    [HttpGet("LongwallParameters")]
    [OpenApiOperation("Get All LongwallParameters", "")]
    public async Task<IActionResult> GetAllLongwallParameters([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallParametersQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("LongwallParameters/export")]
    [OpenApiOperation("Export LongwallParameters", "")]
    public async Task<IActionResult> ExportLongwallParameters()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallParametersQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Thong_so_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallParameters/import")]
    [OpenApiOperation("Import LongwallParameters", "")]
    public async Task<IActionResult> ImportLongwallParameters([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallParametersExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("LongwallParameters/{id:guid}")]
    [OpenApiOperation("Get LongwallParameters By Id", "")]
    public async Task<IActionResult> GetLongwallParametersById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallParametersByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallParameters")]
    [OpenApiOperation("Create New LongwallParameters", "")]
    public async Task<IActionResult> CreateLongwallParameters([FromBody] CreateLongwallParametersDto createModel)
    {
        var result = await Mediator.Send(new CreateLongwallParametersCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("LongwallParameters")]
    [OpenApiOperation("Update LongwallParameters", "")]
    public async Task<IActionResult> UpdateLongwallParameters([FromBody] LongwallParametersDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallParametersCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallParameters/{deleteId:guid}")]
    [OpenApiOperation("Delete LongwallParameters", "")]
    public async Task<IActionResult> DeleteLongwallParameters([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallParameters")]
    [OpenApiOperation("Delete Many LongwallParameters", "")]
    public async Task<IActionResult> DeleteLongwallParametersList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLongwallParametersListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region CuttingThickness

    [HttpGet("CuttingThickness")]
    [OpenApiOperation("Get All CuttingThickness", "")]
    public async Task<IActionResult> GetAllCuttingThickness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllCuttingThicknessQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("CuttingThickness/export")]
    [OpenApiOperation("Export CuttingThickness", "")]
    public async Task<IActionResult> ExportCuttingThickness()
    {
        var fileByte = await Mediator.Send(new ExportExcelCuttingThicknessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chieu_day_lop_khau.xlsx");
        return result;
    }

    [HttpPost("CuttingThickness/import")]
    [OpenApiOperation("Import CuttingThickness", "")]
    public async Task<IActionResult> ImportCuttingThickness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportCuttingThicknessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("CuttingThickness/{id:guid}")]
    [OpenApiOperation("Get CuttingThickness By Id", "")]
    public async Task<IActionResult> GetCuttingThicknessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetCuttingThicknessByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("CuttingThickness")]
    [OpenApiOperation("Create New CuttingThickness", "")]
    public async Task<IActionResult> CreateCuttingThicknesss([FromBody] CreateCuttingThicknessDto createModel)
    {
        var result = await Mediator.Send(new CreateCuttingThicknessCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("CuttingThickness")]
    [OpenApiOperation("Update CuttingThickness", "")]
    public async Task<IActionResult> UpdateCuttingThickness([FromBody] CuttingThicknessDto updateModel)
    {
        var result = await Mediator.Send(new UpdateCuttingThicknessCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("CuttingThickness/{deleteId:guid}")]
    [OpenApiOperation("Delete CuttingThickness", "")]
    public async Task<IActionResult> DeleteCuttingThickness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("CuttingThickness")]
    [OpenApiOperation("Delete Many CuttingThickness", "")]
    public async Task<IActionResult> DeleteCuttingThicknessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteCuttingThicknessListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Hardness

    [HttpGet("Hardness")]
    [OpenApiOperation("Get All Hardness", "")]
    public async Task<IActionResult> GetAllHardness([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Hardness>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Hardness/export")]
    [OpenApiOperation("Export Hardness", "")]
    public async Task<IActionResult> ExportHardness()
    {
        var fileByte = await Mediator.Send(new ExportExcelHardnessQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Do_kien_co_than_da.xlsx");
        return result;
    }

    [HttpPost("Hardness/import")]
    [OpenApiOperation("Import Hardness", "")]
    public async Task<IActionResult> ImportHardness([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportHardnessExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Hardness/{Id:guid}")]
    [OpenApiOperation("Get Hardness By Id", "")]
    public async Task<IActionResult> GetHardnessById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Hardness>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Hardness")]
    [OpenApiOperation("Create New Hardness", "")]
    public async Task<IActionResult> CreateHardness([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Hardness>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Hardness")]
    [OpenApiOperation("Update Hardness", "")]
    public async Task<IActionResult> UpdateHardness([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Hardness>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Hardness/{deleteId:guid}")]
    [OpenApiOperation("Delete Hardness", "")]
    public async Task<IActionResult> DeleteHardness([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Hardness>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Hardness")]
    [OpenApiOperation("Delete Many Hardness", "")]
    public async Task<IActionResult> DeleteHardnessList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Hardness>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region InsertItem

    [HttpGet("InsertItem")]
    [OpenApiOperation("Get All InsertItem", "")]
    public async Task<IActionResult> GetAllInsertItem([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<InsertItem>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("InsertItem/export")]
    [OpenApiOperation("Export InsertItem", "")]
    public async Task<IActionResult> ExportInsertItem()
    {
        var fileByte = await Mediator.Send(new ExportExcelInsertItemQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chen.xlsx");
        return result;
    }

    [HttpPost("InsertItem/import")]
    [OpenApiOperation("Import InsertItem", "")]
    public async Task<IActionResult> ImportInsertItem([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportInsertItemExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("InsertItem/{id:guid}")]
    [OpenApiOperation("Get InsertItem By Id", "")]
    public async Task<IActionResult> GetInsertItemById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<InsertItem>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("InsertItem")]
    [OpenApiOperation("Create New InsertItem", "")]
    public async Task<IActionResult> CreateInsertItem([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<InsertItem>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("InsertItem")]
    [OpenApiOperation("Update InsertItem", "")]
    public async Task<IActionResult> UpdateInsertItem([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<InsertItem>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("InsertItem/{deleteId:guid}")]
    [OpenApiOperation("Delete InsertItem", "")]
    public async Task<IActionResult> DeleteInsertItem([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<InsertItem>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("InsertItem")]
    [OpenApiOperation("Delete Many Hardness", "")]
    public async Task<IActionResult> DeleteInsertItemList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<InsertItem>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductionOrder

    [HttpGet("ProductionOrder")]
    [OpenApiOperation("Get All ProductionOrder", "")]
    public async Task<IActionResult> GetAllProductionOrder([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductionOrderQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductionOrder/{id:guid}")]
    [OpenApiOperation("Get ProductionOrder By Id", "")]
    public async Task<IActionResult> GetProductionOrderById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductionOrderByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("ProductionOrder")]
    [OpenApiOperation("Create New ProductionOrder", "")]
    public async Task<IActionResult> CreateProductionOrder([FromBody] CreateProductionOrderDto createModel)
    {
        var result = await Mediator.Send(new CreateProductionOrderCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("ProductionOrder")]
    [OpenApiOperation("Update ProductionOrder", "")]
    public async Task<IActionResult> UpdateProductionOrder([FromBody] ProductionOrderDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductionOrderCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("ProductionOrder")]
    [OpenApiOperation("Delete Many ProductionOrder", "")]
    public async Task<IActionResult> DeleteProductionOrderList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductionOrderListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Technology

    [HttpGet("Technology")]
    [OpenApiOperation("Get All Technology", "")]
    public async Task<IActionResult> GetAllTechnology([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<Technology>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Technology/export")]
    [OpenApiOperation("Export Technology", "")]
    public async Task<IActionResult> ExportTechnology()
    {
        var fileByte = await Mediator.Send(new ExportExcelTechnologyQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cong_nghe.xlsx");
        return result;
    }

    [HttpPost("Technology/import")]
    [OpenApiOperation("Import Technology", "")]
    public async Task<IActionResult> ImportTechnology([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTechnologyExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Technology/{id:guid}")]
    [OpenApiOperation("Get Technology By Id", "")]
    public async Task<IActionResult> GetTechnologyById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<Technology>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Technology")]
    [OpenApiOperation("Create New Technology", "")]
    public async Task<IActionResult> CreateTechnology([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<Technology>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Technology")]
    [OpenApiOperation("Update Technology", "")]
    public async Task<IActionResult> UpdateTechnology([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<Technology>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Technology/{deleteId:guid}")]
    [OpenApiOperation("Delete Technology", "")]
    public async Task<IActionResult> DeleteTechnology([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<Technology>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Technology")]
    [OpenApiOperation("Delete Many Technology", "")]
    public async Task<IActionResult> DeleteTechnologyList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<Technology>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SeamFace

    [HttpGet("SeamFace")]
    [OpenApiOperation("Get All SeamFace", "")]
    public async Task<IActionResult> GetAllSeamFace([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<SeamFace>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SeamFace/export")]
    [OpenApiOperation("Export SeamFace", "")]
    public async Task<IActionResult> ExportSeamFace()
    {
        var fileByte = await Mediator.Send(new ExportExcelSeamFaceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mat_nut.xlsx");
        return result;
    }

    [HttpPost("SeamFace/import")]
    [OpenApiOperation("Import SeamFace", "")]
    public async Task<IActionResult> ImportSeamFace([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSeamFaceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SeamFace/{id:guid}")]
    [OpenApiOperation("Get SeamFace By Id", "")]
    public async Task<IActionResult> GetSeamFaceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SeamFace>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SeamFace")]
    [OpenApiOperation("Create New SeamFace", "")]
    public async Task<IActionResult> CreateSeamFace([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SeamFace>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SeamFace")]
    [OpenApiOperation("Update SeamFace", "")]
    public async Task<IActionResult> UpdateSeamFace([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SeamFace>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SeamFace/{deleteId:guid}")]
    [OpenApiOperation("Delete SeamFace", "")]
    public async Task<IActionResult> DeleteSeamFace([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SeamFace>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SeamFace")]
    [OpenApiOperation("Delete Many SeamFace", "")]
    public async Task<IActionResult> DeleteSeamFaceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SeamFace>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region StoneClampRatio

    [HttpGet("StoneClampRatio")]
    [OpenApiOperation("Get All StoneClampRatio", "")]
    public async Task<IActionResult> GetAllStoneClampRatio([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllStoneClampRatioQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/{id:guid}")]
    [OpenApiOperation("Get StoneClampRatio By Id", "")]
    public async Task<IActionResult> GetStoneClampRatioById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetStoneClampRatioByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("StoneClampRatio/export")]
    [OpenApiOperation("Export StoneClampRatio", "")]
    public async Task<IActionResult> ExportStoneClampRatio()
    {
        var fileByte = await Mediator.Send(new ExportExcelStoneClampRatioQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Ti_le_da_kep.xlsx");
        return result;
    }

    [HttpPost("StoneClampRatio/import")]
    [OpenApiOperation("Import StoneClampRatio", "")]
    public async Task<IActionResult> ImportStoneClampRatio([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportStoneClampRatioExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpPost("StoneClampRatio")]
    [OpenApiOperation("Create New StoneClampRatio", "")]
    public async Task<IActionResult> CreateStoneClampRatio([FromBody] CreateStoneClampRatioDto createModel)
    {
        var result = await Mediator.Send(new CreateStoneClampRatioCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("StoneClampRatio")]
    [OpenApiOperation("Update StoneClampRatio", "")]
    public async Task<IActionResult> UpdateStoneClampRatio([FromBody] UpdateStoneClampRatioDto updateModel)
    {
        var result = await Mediator.Send(new UpdateStoneClampRatioCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("StoneClampRatio/{deleteId:guid}")]
    [OpenApiOperation("Delete StoneClampRatio", "")]
    public async Task<IActionResult> DeleteStoneClampRatio([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("StoneClampRatio")]
    [OpenApiOperation("Delete Many StoneClampRatio", "")]
    public async Task<IActionResult> DeleteStoneClampRatioList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteStoneClampRatioListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region SupportStep

    [HttpGet("SupportStep")]
    [OpenApiOperation("Get All SupportStep", "")]
    public async Task<IActionResult> GetAllSupportStep([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMetricQuery<SupportStep>(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SupportStep/export")]
    [OpenApiOperation("Export SupportStep", "")]
    public async Task<IActionResult> ExportSupportStep()
    {
        var fileByte = await Mediator.Send(new ExportExcelSupportStepQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Buoc_chong.xlsx");
        return result;
    }

    [HttpPost("SupportStep/import")]
    [OpenApiOperation("Import SupportStep", "")]
    public async Task<IActionResult> ImportSupportStep([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSupportStepExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("SupportStep/{id:guid}")]
    [OpenApiOperation("Get SupportStep By Id", "")]
    public async Task<IActionResult> GetSupportStepById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMetricByIdQuery<SupportStep>(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("SupportStep")]
    [OpenApiOperation("Create New SupportStep", "")]
    public async Task<IActionResult> CreateSupportStep([FromBody] CreateMetricDto createModel)
    {
        var result = await Mediator.Send(new CreateMetricCommand<SupportStep>(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("SupportStep")]
    [OpenApiOperation("Update SupportStep", "")]
    public async Task<IActionResult> UpdateSupportStep([FromBody] MetricDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMetricCommand<SupportStep>(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("SupportStep/{deleteId:guid}")]
    [OpenApiOperation("Delete SupportStep", "")]
    public async Task<IActionResult> DeleteSupportStep([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMetricCommand<SupportStep>(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SupportStep")]
    [OpenApiOperation("Delete Many SupportStep", "")]
    public async Task<IActionResult> DeleteSupportStepList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMetricListCommand<SupportStep>(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region Product

    [HttpGet("Product")]
    [OpenApiOperation("Get All Product", "")]
    public async Task<IActionResult> GetAllProduct([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllProductQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Product/export")]
    [OpenApiOperation("Export Product", "")]
    public async Task<IActionResult> ExportProduct()
    {
        var fileByte = await Mediator.Send(new ExportExcelProductQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "San_pham.xlsx");
        return result;
    }

    [HttpPost("Product/import")]
    [OpenApiOperation("Import Product", "")]
    public async Task<IActionResult> ImportProduct([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportProductExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("Product/{id:guid}")]
    [OpenApiOperation("Get Product By Id", "")]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Product")]
    [OpenApiOperation("Create New Product", "")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto createModel)
    {
        var result = await Mediator.Send(new CreateProductCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Product")]
    [OpenApiOperation("Update Product", "")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Product/{deleteId:guid}")]
    [OpenApiOperation("Delete Product", "")]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteProductCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Product")]
    [OpenApiOperation("Delete Many Product", "")]
    public async Task<IActionResult> DeleteProductList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region NormFactor

    [HttpGet("NormFactor")]
    [OpenApiOperation("Get All NormFactor", "")]
    public async Task<IActionResult> GetAllNormFactor([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllNormFactorQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("NormFactor/{id:guid}")]
    [OpenApiOperation("Get NormFactor By Id", "")]
    public async Task<IActionResult> GetNormFactorById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetNormFactorByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("NormFactor")]
    [OpenApiOperation("Create New NormFactor", "")]
    public async Task<IActionResult> CreateNormFactor([FromBody] CreateNormFactorDto createModel)
    {
        var result = await Mediator.Send(new CreateNormFactorCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("NormFactor")]
    [OpenApiOperation("Update NormFactor", "")]
    public async Task<IActionResult> UpdateNormFactor([FromBody] UpdateNormFactorDto updateModel)
    {
        var result = await Mediator.Send(new UpdateNormFactorCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("NormFactor")]
    [OpenApiOperation("Delete Many NormFactor", "")]
    public async Task<IActionResult> DeleteNormFactorList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteNormFactorListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion
}
