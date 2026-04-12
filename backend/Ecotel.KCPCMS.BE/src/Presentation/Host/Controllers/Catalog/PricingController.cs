using Application.Catalog.Pricing.ActualElectricityCost.Commands;
using Application.Catalog.Pricing.ActualElectricityCost.Queries;
using Application.Catalog.Pricing.AdjustmentElectricityCost.Queries;
using Application.Catalog.Pricing.AdjustmentMaterialCost.Queries;
using Application.Catalog.Pricing.AdjustmnetMaintainCost.Queries;
using Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;
using Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;
using Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;
using Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;
using Application.Catalog.Pricing.LumpSumFinalSettlement.Commands;
using Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;
using Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;
using Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;
using Application.Catalog.Pricing.MaterialUnitPrice.Commands;
using Application.Catalog.Pricing.MaterialUnitPrice.Queries;
using Application.Catalog.Pricing.PlannedElectricityCost.Commands;
using Application.Catalog.Pricing.PlannedElectricityCost.Queries;
using Application.Catalog.Pricing.PlannedMaintainCost.Commands;
using Application.Catalog.Pricing.PlannedMaintainCost.Queries;
using Application.Catalog.Pricing.PlannedMaterialCost.Commands;
using Application.Catalog.Pricing.PlannedMaterialCost.Queries;
using Application.Catalog.Pricing.ProductUnitPrice.Commands;
using Application.Catalog.Pricing.ProductUnitPrice.Queries;
using Application.Catalog.Pricing.SlideUnitPrice.Commands;
using Application.Catalog.Pricing.SlideUnitPrice.Queries;
using Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Commands;
using Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Queries;
using Application.Dto.Catalog.ActualElectricityCost;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using Application.Dto.Catalog.MaterialUnitPrice;
using Application.Dto.Catalog.PlannedElectricityCost;
using Application.Dto.Catalog.PlannedMaintainCost;
using Application.Dto.Catalog.PlannedMaterialCost;
using Application.Dto.Catalog.ProductUnitPrice;
using Application.Dto.Catalog.SlideUnitPrice;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Common.Enums;
using Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class PricingController : BaseNoAuthController
{
    #region MaterialUnitPrice

    [HttpGet("MaterialUnitPrice")]
    [OpenApiOperation("Get All MaterialUnitPrice", "")]
    public async Task<IActionResult> GetAllMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaterialUnitPriceQuery(
            pageIndex,
            pageSize,
            search,
            ignorePagination,
            TunnelExcavationTrimingUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("MaterialUnitPrice/All")]
    [OpenApiOperation("Get All MaterialUnitPrice (All Types - Longwall, TunnelExcavation)", "")]
    public async Task<IActionResult> GetAllMaterialUnitPricesUnified([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] MaterialUnitPriceType? materialType = null)
    {
        var result = await Mediator.Send(new GetAllMaterialUnitPricesUnifiedQuery(pageIndex, pageSize, search, ignorePagination, materialType));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("MaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get MaterialUnitPrice By Id", "")]
    public async Task<IActionResult> GetMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("MaterialUnitPrice")]
    [OpenApiOperation("Update MaterialUnitPrice", "")]
    public async Task<IActionResult> UpdateMaterialUnitPrice([FromBody] UpdateMaterialUnitPriceDto updateModel)
    {
        updateModel.Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("MaterialUnitPrice")]
    [OpenApiOperation("Create New MaterialUnitPrice", "")]
    public async Task<IActionResult> CreateMaterialUnitPrice([FromBody] CreateMaterialUnitPriceDto createModel)
    {
        createModel.Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new CreateMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("MaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete MaterialUnitPrice", "")]
    public async Task<IActionResult> DeleteMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("MaterialUnitPriceList")]
    [OpenApiOperation("Delete MaterialUnitPrice", "")]
    public async Task<IActionResult> DeleteMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("MaterialUnitPrice/export")]
    [OpenApiOperation("Export MaterialUnitPrice", "")]
    public async Task<IActionResult> ExportMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelMaterialUnitPriceQuery(TunnelExcavationTrimingUnitPriceType.TunnelExcavation));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dao_lo_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("MaterialUnitPrice/import")]
    [OpenApiOperation("Import MaterialUnitPrice", "")]
    public async Task<IActionResult> ImportMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportMaterialUnitPriceExcelCommand(importModel.FormFile, TunnelExcavationTrimingUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Get All Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> GetAllTrimmingMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaterialUnitPriceQuery(
            pageIndex,
            pageSize,
            search,
            ignorePagination,
            TunnelExcavationTrimingUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TrimmingMaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Trimming MaterialUnitPrice By Id (Xén lò)", "")]
    public async Task<IActionResult> GetTrimmingMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Update Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> UpdateTrimmingMaterialUnitPrice([FromBody] UpdateMaterialUnitPriceDto updateModel)
    {
        updateModel.Type = TunnelExcavationTrimingUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Create New Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> CreateTrimmingMaterialUnitPrice([FromBody] CreateMaterialUnitPriceDto createModel)
    {
        createModel.Type = TunnelExcavationTrimingUnitPriceType.Trimming;
        var result = await Mediator.Send(new CreateMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("TrimmingMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> DeleteTrimmingMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TrimmingMaterialUnitPriceList")]
    [OpenApiOperation("Delete Trimming MaterialUnitPrice List (Xén lò)", "")]
    public async Task<IActionResult> DeleteTrimmingMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingMaterialUnitPrice/export")]
    [OpenApiOperation("Export Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> ExportTrimmingMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelMaterialUnitPriceQuery(TunnelExcavationTrimingUnitPriceType.Trimming));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Xen_lo_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("TrimmingMaterialUnitPrice/import")]
    [OpenApiOperation("Import Trimming MaterialUnitPrice (Xén lò)", "")]
    public async Task<IActionResult> ImportTrimmingMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportMaterialUnitPriceExcelCommand(importModel.FormFile, TunnelExcavationTrimingUnitPriceType.Trimming));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region TunnelSupportAndDrillingMaterialUnitPrice - ĐÀO LÒ  CHỐNG NEO, BÊ TÔNG PHUN VÀ KHOAN THĂM DÒ

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Get All Tunnel Support And Drilling MaterialUnitPrice", "")]
    public async Task<IActionResult> GetAllTunnelSupportAndDrillingMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Tunnel Support And Drilling MaterialUnitPrice By Id", "")]
    public async Task<IActionResult> GetTunnelSupportAndDrillingMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelSupportAndDrillingMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Update Tunnel Support And Drilling MaterialUnitPrice", "")]
    public async Task<IActionResult> UpdateTunnelSupportAndDrillingMaterialUnitPrice([FromBody] UpdateTunnelSupportAndDrillingMaterialUnitPrice updateModel)
    {
        var result = await Mediator.Send(new UpdateTunnelSupportAndDrillingMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Create New Tunnel Support And Drilling MaterialUnitPrice", "")]
    public async Task<IActionResult> CreateTunnelSupportAndDrillingMaterialUnitPrice([FromBody] CreateTunnelSupportAndDrillingMaterialUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateTunnelSupportAndDrillingMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("TunnelSupportAndDrillingMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel Support And Drilling MaterialUnitPrice", "")]
    public async Task<IActionResult> DeleteTunnelSupportAndDrillingMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelSupportAndDrillingMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TunnelSupportAndDrillingMaterialUnitPriceList")]
    [OpenApiOperation("Delete Multiple Tunnel Support And Drilling MaterialUnitPrice", "")]
    public async Task<IActionResult> DeleteTunnelSupportAndDrillingMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteTunnelSupportAndDrillingMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice/export")]
    [OpenApiOperation("Export TunnelSupportAndDrillingMaterialUnitPrice", "")]
    public async Task<IActionResult> ExportTunnelSupportAndDrillingMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelSupportAndDrillingMaterialUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chong_xen_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("TunnelSupportAndDrillingMaterialUnitPrice/import")]
    [OpenApiOperation("Import TunnelSupportAndDrillingMaterialUnitPrice", "")]
    public async Task<IActionResult> ImportTunnelSupportAndDrillingMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region LongwallMaterialUnitPrice - Lò chợ

    [HttpGet("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Get All Longwall MaterialUnitPrice (Lò chợ)", "")]
    public async Task<IActionResult> GetAllLongwallMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallMaterialUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("LongwallMaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Longwall MaterialUnitPrice By Id (Lò chợ)", "")]
    public async Task<IActionResult> GetLongwallMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Update Longwall MaterialUnitPrice (Lò chợ)", "")]
    public async Task<IActionResult> UpdateLongwallMaterialUnitPrice([FromBody] UpdateLongwallMaterialUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Create New Longwall MaterialUnitPrice (Lò chợ)", "")]
    public async Task<IActionResult> CreateLongwallMaterialUnitPrice([FromBody] CreateLongwallMaterialUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateLongwallMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("LongwallMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall MaterialUnitPrice (Lò chợ)", "")]
    public async Task<IActionResult> DeleteLongwallMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallMaterialUnitPriceList")]
    [OpenApiOperation("Delete Multiple Longwall MaterialUnitPrice (Lò chợ)", "")]
    public async Task<IActionResult> DeleteLongwallMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLongwallMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallMaterialUnitPrice/export")]
    [OpenApiOperation("Export LongwallMaterialUnitPrice", "")]
    public async Task<IActionResult> ExportLongwallMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallMaterialUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Lo_cho_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("LongwallMaterialUnitPrice/import")]
    [OpenApiOperation("Import LongwallMaterialUnitPrice", "")]
    public async Task<IActionResult> ImportLongwallMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallMaterialUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    #endregion

    #region SlideUnitPrice

    [HttpGet("SlideUnitPrice")]
    [OpenApiOperation("Get All SlideUnitPrice", "")]
    public async Task<IActionResult> GetAllSlideUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSlideUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SlideUnitPrice/Details")]
    [OpenApiOperation("Get All SlideUnitPrice Detail list", "")]
    public async Task<IActionResult> GetAllSlideUnitPriceAssignmentCode([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSlideUnitPriceAssignmentCodeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SlideUnitPrice/{id:guid}")]
    [OpenApiOperation("Get SlideUnitPrice By Id", "")]
    public async Task<IActionResult> GetSlideUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetSlideUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("SlideUnitPrice")]
    [OpenApiOperation("Update SlideUnitPrice", "")]
    public async Task<IActionResult> UpdateSlideUnitPrice([FromBody] UopdateSlideUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateSlideUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("SlideUnitPrice")]
    [OpenApiOperation("Create New SlideUnitPrice", "")]
    public async Task<IActionResult> CreateSlideUnitPrice([FromBody] CreateSlideUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateSlideUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("SlideUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete SlideUnitPrice", "")]
    public async Task<IActionResult> DeleteSlideUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteSlideUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SlideUnitPrice")]
    [OpenApiOperation("Delete SlideUnitPrice List", "")]
    public async Task<IActionResult> DeleteSlideUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteSlideUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("SlideUnitPrice/export")]
    [OpenApiOperation("Export SlideUnitPrice", "")]
    public async Task<IActionResult> ExportSlideUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelSlideUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dao_lo_don_gia_mang_truot.xlsx");
        return result;
    }

    [HttpPost("SlideUnitPrice/import")]
    [OpenApiOperation("Import SlideUnitPrice", "")]
    public async Task<IActionResult> ImportSlideUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSlideUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region MaintainUnitPrice

    [HttpGet("MaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All MaintainUnitPriceEquipment", "")]
    public async Task<IActionResult> GetAllMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] MaintainUnitPriceType? maintainType = null)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, maintainType));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("MaintainUnitPriceEquipment")]
    [OpenApiOperation("Create New MaintainUnitPriceEquipment", "")]
    public async Task<IActionResult> CreateMaintainUnitPriceEquipment([FromBody] IList<Application.Dto.Catalog.MaintainUnitPrice.CreateMaintainUnitPriceEquipmentDto> createModel)
    {
        var result = await Mediator.Send(new CreateMaintainUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("MaintainUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get MaintainUnitPriceEquipment By Equipment Id", "")]
    public async Task<IActionResult> GetMaintainUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaintainUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("MaintainUnitPriceEquipment/equipments")]
    [OpenApiOperation("Get Equipments By MaintainUnitPriceEquipment Ids", "")]
    public async Task<IActionResult> GetEquipmentsByMaintainUnitPriceEquipmentIds(
        [FromBody] IList<Guid> maintainUnitPriceEquipmentIds,
        [FromQuery] DateTime? date = null)
    {
        var result = await Mediator.Send(new GetEquipmentsByMaintainUnitPriceEquipmentIdsQuery(maintainUnitPriceEquipmentIds, date));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("MaintainUnitPriceEquipment")]
    [OpenApiOperation("Update MaintainUnitPriceEquipment", "")]
    public async Task<IActionResult> UpdateMaintainUnitPriceEquipment([FromBody] UpdateMaintainUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateMaintainUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("MaintainUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete MaintainUnitPriceEquipment", "")]
    public async Task<IActionResult> DeleteMaintainUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("MaintainUnitPriceEquipment")]
    [OpenApiOperation("Delete MaintainUnitPriceEquipment List", "")]
    public async Task<IActionResult> DeleteMaintainUnitPriceEquipmenteList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Tunnel MaintainUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> ExportTunnelMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_dao_lo.xlsx");
        return result;
    }

    [HttpPost("TunnelMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Tunnel MaintainUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> ImportTunnelMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> GetAllTrimmingMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, MaintainUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Create New Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> CreateTrimmingMaintainUnitPriceEquipment([FromBody] IList<Application.Dto.Catalog.MaintainUnitPrice.CreateMaintainUnitPriceEquipmentDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = MaintainUnitPriceType.Trimming;
        }

        var result = await Mediator.Send(new CreateMaintainUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("TrimmingMaintainUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Trimming MaintainUnitPriceEquipment By Id (Xén lò)", "")]
    public async Task<IActionResult> GetTrimmingMaintainUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaintainUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Update Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> UpdateTrimmingMaintainUnitPriceEquipment([FromBody] UpdateMaintainUnitPriceDto updateModel)
    {
        updateModel.Type = MaintainUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateMaintainUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TrimmingMaintainUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> DeleteTrimmingMaintainUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> ExportTrimmingMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTrimmingMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_xen_lo.xlsx");
        return result;
    }

    [HttpPost("TrimmingMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> ImportTrimmingMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTrimmingMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("LongwallMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ExportLongwallMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ImportLongwallMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region TunnelElectricityUnitPriceEquipment (Đào lò)

    [HttpGet("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> GetAllTunnelElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> CreateTunnelElectricityUnitPriceEquipment([FromBody] IList<CreateElectricityUnitPriceEquipmentDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = ElectricityUnitPriceType.TunnelExcavation;
        }

        var result = await Mediator.Send(new CreateElectricityUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("TunnelElectricityUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Tunnel ElectricityUnitPriceEquipment By Id (Đào lò)", "")]
    public async Task<IActionResult> GetTunnelElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelElectricityUnitPriceEquipmentByIdQuery(id, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> UpdateTunnelElectricityUnitPriceEquipment([FromBody] UpdateElectricityUnitPriceEquipmentDto updateModel)
    {
        updateModel.Type = ElectricityUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TunnelElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    public async Task<IActionResult> DeleteTunnelElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelElectricityUnitPriceEquipmentCommand(deleteId, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Tunnel ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ExportTunnelElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelElectricityUnitPriceEquipmentQuery(ElectricityUnitPriceType.TunnelExcavation));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_lo_cho.xlsx");
        return result;
    }

    [HttpPost("TunnelElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Tunnel ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ImportTunnelElectricityUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelElectricityUnitPriceEquipmentExcelCommand(importModel.FormFile, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> GetAllTrimmingElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> CreateTrimmingElectricityUnitPriceEquipment([FromBody] IList<CreateElectricityUnitPriceEquipmentDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = ElectricityUnitPriceType.Trimming;
        }

        var result = await Mediator.Send(new CreateElectricityUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("TrimmingElectricityUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Trimming ElectricityUnitPriceEquipment By Id (Xén lò)", "")]
    public async Task<IActionResult> GetTrimmingElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelElectricityUnitPriceEquipmentByIdQuery(id, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> UpdateTrimmingElectricityUnitPriceEquipment([FromBody] UpdateElectricityUnitPriceEquipmentDto updateModel)
    {
        updateModel.Type = ElectricityUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TrimmingElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> DeleteTrimmingElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelElectricityUnitPriceEquipmentCommand(deleteId, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> ExportTrimmingElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelElectricityUnitPriceEquipmentQuery(ElectricityUnitPriceType.Trimming));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_xen_lo.xlsx");
        return result;
    }

    [HttpPost("TrimmingElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    public async Task<IActionResult> ImportTrimmingElectricityUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelElectricityUnitPriceEquipmentExcelCommand(importModel.FormFile, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region LongwallElectricityUnitPriceEquipment (Lò chợ)

    [HttpGet("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> GetAllLongwallElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> CreateLongwallElectricityUnitPriceEquipment([FromBody] IList<CreateLongwallElectricityUnitPriceEquipmentDto> createModel)
    {
        var result = await Mediator.Send(new CreateLongwallElectricityUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("LongwallElectricityUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Longwall ElectricityUnitPriceEquipment By Id (Lò chợ)", "")]
    public async Task<IActionResult> GetLongwallElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallElectricityUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> UpdateLongwallElectricityUnitPriceEquipment([FromBody] UpdateLongwallElectricityUnitPriceEquipmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> DeleteLongwallElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallElectricityUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ExportLongwallElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallElectricityUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    public async Task<IActionResult> ImportLongwallElectricityUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallElectricityUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region ElectricityUnitPriceEquipment (Shared - All Types)

    [HttpGet("ElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All ElectricityUnitPriceEquipment (All Types)", "")]
    public async Task<IActionResult> GetAllElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ElectricityUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get ElectricityUnitPriceEquipment By Id (All Types)", "")]
    public async Task<IActionResult> GetElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetElectricityUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpDelete("ElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete ElectricityUnitPriceEquipment (All Types)", "")]
    public async Task<IActionResult> DeleteElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteElectricityUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }


    [HttpDelete("ElectricityUnitPriceEquipment")]
    [OpenApiOperation("Delete ElectricityUnitPriceEquipment List (All Types)", "")]
    public async Task<IActionResult> DeleteElectricityUnitPriceEquipmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteElectricityUnitPriceEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductUnitPrice

    [HttpGet("ProductUnitPrice")]
    [OpenApiOperation("Get All ProductUnitPrice", "")]
    public async Task<IActionResult> GetAllProductUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] ProductUnitPriceScenarioType scenarioType = ProductUnitPriceScenarioType.Plan, [FromQuery] Guid? departmentId = null)
    {
        var result = await Mediator.Send(new GetAllProductUnitPriceQuery(pageIndex, pageSize, search, ignorePagination, scenarioType, departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductUnitPrice/Planned/{id:guid}")]
    [OpenApiOperation("Get ProductUnitPrice By Id", "")]
    public async Task<IActionResult> GetPlannedProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductUnitPrice/Actual/{id:guid}")]
    [OpenApiOperation("Get ProductActualUnitPrice By Id", "")]
    public async Task<IActionResult> GetActualProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetActualProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductUnitPrice/Adjustment/{id:guid}")]
    [OpenApiOperation("Get ProductAdjustmentUnitPrice By Id", "")]
    public async Task<IActionResult> GetAdjustmentProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ProductUnitPrice")]
    [OpenApiOperation("Update ProductUnitPrice", "")]
    public async Task<IActionResult> UpdateProductUnitPrice([FromBody] UpdateProductUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPut("ProductUnitPrice/Adjustment")]
    [OpenApiOperation("Update Adjustment ProductUnitPrice", "")]
    public async Task<IActionResult> UpdateAdjustmentProductUnitPrice([FromBody] UpdateAdjustmentProductUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentProductUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("ProductUnitPrice")]
    [OpenApiOperation("Create New ProductUnitPrice", "")]
    public async Task<IActionResult> CreateProductUnitPrice([FromBody] CreateProductUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateProductUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("ProductUnitPrice")]
    [OpenApiOperation("Delete ProductUnitPrice List", "")]
    public async Task<IActionResult> DeleteProductUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("ProductUnitPrice/export-adjustment-electricity-maintain-report")]
    [OpenApiOperation("Export Adjustment Electricity And Maintain Report", "Export Bảng tính đơn giá SCTX và điện năng")]
    public async Task<IActionResult> ExportAdjustmentElectricityAndMaintainReport(
        [FromQuery] string? month,
        [FromQuery] string? year,
        [FromQuery] Guid? processGroupId,
        [FromQuery] ProductUnitPriceScenarioType scenarioType = ProductUnitPriceScenarioType.Adjustment)
    {
        var result = await Mediator.Send(new ExportAdjustmentElectricityAndMaintainReportExcelQuery(month, year, processGroupId, scenarioType));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    #endregion

    #region PlannedMaterialCost

    [HttpGet("PlannedMaterialCost/{id:guid}")]
    [OpenApiOperation("Get PlannedMaterialCost By Id", "")]
    public async Task<IActionResult> GetPlannedMaterialCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedMaterialCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedMaterialCost")]
    [OpenApiOperation("Update PlannedMaterialCost", "")]
    public async Task<IActionResult> UpdatePlannedMaterialCost([FromBody] UpdatePlannedMaterialCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedMaterialCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedMaterialCost")]
    [OpenApiOperation("Create New PlannedMaterialCost", "")]
    public async Task<IActionResult> CreatePlannedMaterialCost([FromBody] CreatePlannedMaterialCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedMaterialCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedMaterialCost")]
    [OpenApiOperation("Delete PlannedMaterialCost List", "")]
    public async Task<IActionResult> DeletePlannedMaterialCostList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePlannedMaterialCostListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region PlannedMaintainCost

    [HttpGet("PlannedMaintainCost/{id:guid}")]
    [OpenApiOperation("Get PlannedMaintainCost By Id", "")]
    public async Task<IActionResult> GetPlannedMaintainCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedMaintainCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedMaintainCost")]
    [OpenApiOperation("Update PlannedMaintainCost", "")]
    public async Task<IActionResult> UpdatePlannedMaintainCost([FromBody] UpdatePlannedMaintainCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedMaintainCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedMaintainCost")]
    [OpenApiOperation("Create New PlannedMaintainCost", "")]
    public async Task<IActionResult> CreatePlannedMaintainCost([FromBody] CreatePlannedMaintainCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedMaintainCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedMaintainCost")]
    [OpenApiOperation("Delete PlannedMaintainCost List", "")]
    public async Task<IActionResult> DeletePlannedMaintainCostList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePlannedMaintainCostListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region PlannedElectricityCost
    [HttpGet("PlannedElectricityCost/{id:guid}")]
    [OpenApiOperation("Get PlannedElectricityCost By Id", "")]
    public async Task<IActionResult> GetPlannedElectricityCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedElectricityCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedElectricityCost")]
    [OpenApiOperation("Update PlannedElectricityCost", "")]
    public async Task<IActionResult> UpdatePlannedElectricityCost([FromBody] UpdatePlannedElectricityCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedElectricityCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedElectricityCost")]
    [OpenApiOperation("Create New PlannedElectricityCost", "")]
    public async Task<IActionResult> CreatePlannedElectricityCost([FromBody] CreatePlannedElectricityCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedElectricityCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedElectricityCost")]
    [OpenApiOperation("Delete PlannedElectricityCost List", "")]
    public async Task<IActionResult> DeletePlannedElectricityCostList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePlannedElectricityCostListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ActualElectricityCost

    [HttpGet("ActualElectricityCost/{id:guid}")]
    [OpenApiOperation("Get ActualElectricityCost By AcceptanceReport Id", "")]
    public async Task<IActionResult> GetActualElectricityCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetActualElectricityCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ActualElectricityCost")]
    [OpenApiOperation("Update ActualElectricityCost", "")]
    public async Task<IActionResult> UpdateActualElectricityCost([FromBody] UpdateActualElectricityCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdateActualElectricityCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("ActualElectricityCost")]
    [OpenApiOperation("Create New ActualElectricityCost", "")]
    public async Task<IActionResult> CreateActualElectricityCost([FromBody] CreateActualElectricityCostDto createModel)
    {
        var result = await Mediator.Send(new CreateActualElectricityCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    #endregion
    #region AdjustmentCost
    [HttpGet("AdjustmnetMaterialCost/{id:guid}")]
    [OpenApiOperation("Get AdjustmnetMaterialCost By Id", "")]
    public async Task<IActionResult> GetAdjustmnetMaterialCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentMaterialCostByOutputQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentMaintainCost/{id:guid}")]
    [OpenApiOperation("Get AdjustmentMaintainCost By Id", "")]
    public async Task<IActionResult> GetAdjustmentMaintainCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentMaintainCostByOutputIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentElectricityCost/{id:guid}")]
    [OpenApiOperation("Get AdjustmentElectricityCost By Id", "")]
    public async Task<IActionResult> GetAdjustmentElectricityCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentElectricityCostByOutputIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    #endregion

    #region LumpSumFinalSettlement

    [HttpPost("lump-sum-final-settlement/list")]
    [OpenApiOperation("Get Lump Sum Final Settlement List", "")]
    public async Task<IActionResult> GetLumpSumFinalSettlementList([FromBody] LumpSumFinalSettlementListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumFinalSettlementListQuery(request.Month, request.Year, request.ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("lump-sum-final-settlement/month-export")]
    [OpenApiOperation("Export Lump Sum Final Settlement Month Excel", "")]
    public async Task<IActionResult> ExportLumpSumFinalSettlementMonthExcel(
        [FromQuery] string month,
        [FromQuery] string year,
        [FromQuery] string? processGroupId,
        [FromQuery] string? search)
    {
        var result = await Mediator.Send(new ExportLumpSumFinalSettlementMonthExcelQuery(month, year, processGroupId, search));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    [HttpPost("lump-sum-final-settlement/quarter-list")]
    [OpenApiOperation("Get Lump Sum Final Settlement Quarter List", "")]
    public async Task<IActionResult> GetLumpSumFinalSettlementQuarterList([FromBody] LumpSumFinalSettlementQuarterListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumFinalSettlementQuarterListQuery(request.Quarter, request.Year, request.ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("lump-sum-final-settlement/quarter-custom-cost/list")]
    [OpenApiOperation("Get Lump Sum Quarter Custom Cost List", "")]
    public async Task<IActionResult> GetLumpSumQuarterCustomCostList([FromBody] LumpSumQuarterCustomCostListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumQuarterCustomCostListQuery(request.Quarter, request.Year, request.ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("lump-sum-final-settlement/quarter-export")]
    [OpenApiOperation("Export Lump Sum Final Settlement Quarter Excel", "")]
    public async Task<IActionResult> ExportLumpSumFinalSettlementQuarterExcel(
        [FromQuery] string quarter,
        [FromQuery] string year,
        [FromQuery] string? processGroupId,
        [FromQuery] string? search)
    {
        var result = await Mediator.Send(new ExportLumpSumFinalSettlementQuarterExcelQuery(quarter, year, processGroupId, search));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    [HttpPost("lump-sum-final-settlement/quarter-custom-cost")]
    [OpenApiOperation("Create Lump Sum Quarter Custom Cost", "")]
    public async Task<IActionResult> CreateLumpSumQuarterCustomCost([FromBody] CreateLumpSumQuarterCustomCostRequest request)
    {
        var result = await Mediator.Send(new CreateLumpSumQuarterCustomCostCommand(request));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("lump-sum-final-settlement/quarter-custom-cost")]
    [OpenApiOperation("Update Lump Sum Quarter Custom Cost", "")]
    public async Task<IActionResult> UpdateLumpSumQuarterCustomCost([FromBody] UpdateLumpSumQuarterCustomCostRequest request)
    {
        var result = await Mediator.Send(new UpdateLumpSumQuarterCustomCostCommand(request));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("lump-sum-final-settlement/quarter-custom-cost/{id:guid}")]
    [OpenApiOperation("Delete Lump Sum Quarter Custom Cost", "")]
    public async Task<IActionResult> DeleteLumpSumQuarterCustomCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new DeleteLumpSumQuarterCustomCostCommand(id));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion
}


