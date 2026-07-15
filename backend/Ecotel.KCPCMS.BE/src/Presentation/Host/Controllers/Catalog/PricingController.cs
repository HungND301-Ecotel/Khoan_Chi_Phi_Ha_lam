using Application.Catalog.Pricing.ActualElectricityCost.Commands;
using Application.Catalog.Pricing.ActualElectricityCost.Queries;
using Application.Catalog.Pricing.AdjustmentElectricityCost.Queries;
using Application.Catalog.Pricing.AdjustmentMaterialCost.Queries;
using Application.Catalog.Pricing.AdjustmnetMaintainCost.Queries;
using Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;
using Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;
using Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;
using Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;
using Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;
using Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Queries;
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
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
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
using Infrastructure.Auth.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class PricingController : BaseAuthController
{
    #region MaterialUnitPrice (đào lò)

    [HttpGet("MaterialUnitPrice")]
    [OpenApiOperation("Get All MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.read","Đơn giá, định mức","Đơn giá định mức vật liệu (Đào lò)")]
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
    [HasPermission("pricing.materialunitprice.read", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]

    public async Task<IActionResult> GetAllMaterialUnitPricesUnified([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] MaterialUnitPriceType? materialType = null)
    {
        var result = await Mediator.Send(new GetAllMaterialUnitPricesUnifiedQuery(pageIndex, pageSize, search, ignorePagination, materialType));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("MaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get MaterialUnitPrice By Id", "")]
    [HasPermission("pricing.materialunitprice.read", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]

    public async Task<IActionResult> GetMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("MaterialUnitPrice")]
    [OpenApiOperation("Update MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.update", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]

    public async Task<IActionResult> UpdateMaterialUnitPrice([FromBody] UpdateMaterialUnitPriceDto updateModel)
    {
        updateModel.Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("MaterialUnitPrice")]
    [OpenApiOperation("Create New MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.create", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]

    public async Task<IActionResult> CreateMaterialUnitPrice([FromBody] CreateMaterialUnitPriceDto createModel)
    {
        createModel.Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new CreateMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("MaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.delete", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]

    public async Task<IActionResult> DeleteMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("MaterialUnitPriceList")]
    [OpenApiOperation("Delete MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.delete", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]


    public async Task<IActionResult> DeleteMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("MaterialUnitPrice/export")]
    [OpenApiOperation("Export MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.export", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]


    public async Task<IActionResult> ExportMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelMaterialUnitPriceQuery(TunnelExcavationTrimingUnitPriceType.TunnelExcavation));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dao_lo_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("MaterialUnitPrice/import")]
    [OpenApiOperation("Import MaterialUnitPrice", "")]
    [HasPermission("pricing.materialunitprice.import", "Đơn giá, định mức", "Đơn giá định mức vật liệu (Đào lò)")]


    public async Task<IActionResult> ImportMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportMaterialUnitPriceExcelCommand(importModel.FormFile, TunnelExcavationTrimingUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    #endregion

    #region TrimmingMaterialUnitPrice (xén lò)
    [HttpGet("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Get All Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.read", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]
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
    [HasPermission("pricing.trimmingmaterialunitpricing.read", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]
    public async Task<IActionResult> GetTrimmingMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Update Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.update", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> UpdateTrimmingMaterialUnitPrice([FromBody] UpdateMaterialUnitPriceDto updateModel)
    {
        updateModel.Type = TunnelExcavationTrimingUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("TrimmingMaterialUnitPrice")]
    [OpenApiOperation("Create New Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.create", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> CreateTrimmingMaterialUnitPrice([FromBody] CreateMaterialUnitPriceDto createModel)
    {
        createModel.Type = TunnelExcavationTrimingUnitPriceType.Trimming;
        var result = await Mediator.Send(new CreateMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("TrimmingMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.delete", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> DeleteTrimmingMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TrimmingMaterialUnitPriceList")]
    [OpenApiOperation("Delete Trimming MaterialUnitPrice List (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.delete", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> DeleteTrimmingMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingMaterialUnitPrice/export")]
    [OpenApiOperation("Export Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.export", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> ExportTrimmingMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelMaterialUnitPriceQuery(TunnelExcavationTrimingUnitPriceType.Trimming));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Xen_lo_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("TrimmingMaterialUnitPrice/import")]
    [OpenApiOperation("Import Trimming MaterialUnitPrice (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaterialunitpricing.import", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (Xén lò)")]

    public async Task<IActionResult> ImportTrimmingMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportMaterialUnitPriceExcelCommand(importModel.FormFile, TunnelExcavationTrimingUnitPriceType.Trimming));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region TunnelSupportAndDrillingMaterialUnitPrice - ĐÀO LÒ  CHỐNG NEO, BÊ TÔNG PHUN VÀ KHOAN THĂM DÒ

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Get All Tunnel Support And Drilling MaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> GetAllTunnelSupportAndDrillingMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Tunnel Support And Drilling MaterialUnitPrice By Id", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> GetTunnelSupportAndDrillingMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelSupportAndDrillingMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Update Tunnel Support And Drilling MaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> UpdateTunnelSupportAndDrillingMaterialUnitPrice([FromBody] UpdateTunnelSupportAndDrillingMaterialUnitPrice updateModel)
    {
        var result = await Mediator.Send(new UpdateTunnelSupportAndDrillingMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("TunnelSupportAndDrillingMaterialUnitPrice")]
    [OpenApiOperation("Create New Tunnel Support And Drilling MaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> CreateTunnelSupportAndDrillingMaterialUnitPrice([FromBody] CreateTunnelSupportAndDrillingMaterialUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateTunnelSupportAndDrillingMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("TunnelSupportAndDrillingMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel Support And Drilling MaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> DeleteTunnelSupportAndDrillingMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelSupportAndDrillingMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TunnelSupportAndDrillingMaterialUnitPriceList")]
    [OpenApiOperation("Delete Multiple Tunnel Support And Drilling MaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> DeleteTunnelSupportAndDrillingMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteTunnelSupportAndDrillingMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelSupportAndDrillingMaterialUnitPrice/export")]
    [OpenApiOperation("Export TunnelSupportAndDrillingMaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]

    public async Task<IActionResult> ExportTunnelSupportAndDrillingMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelSupportAndDrillingMaterialUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Lo_neo_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("TunnelSupportAndDrillingMaterialUnitPrice/import")]
    [OpenApiOperation("Import TunnelSupportAndDrillingMaterialUnitPrice", "")]
    [HasPermission("pricing.tunnelsupportanddrillingmaterialunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức lò neo bê tông phun")]
    public async Task<IActionResult> ImportTunnelSupportAndDrillingMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region LongwallMaterialUnitPrice - Lò chợ

    [HttpGet("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Get All Longwall MaterialUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]
    public async Task<IActionResult> GetAllLongwallMaterialUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallMaterialUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("LongwallMaterialUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Longwall MaterialUnitPrice By Id (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> GetLongwallMaterialUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallMaterialUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Update Longwall MaterialUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> UpdateLongwallMaterialUnitPrice([FromBody] UpdateLongwallMaterialUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallMaterialUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("LongwallMaterialUnitPrice")]
    [OpenApiOperation("Create New Longwall MaterialUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> CreateLongwallMaterialUnitPrice([FromBody] CreateLongwallMaterialUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateLongwallMaterialUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("LongwallMaterialUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall MaterialUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> DeleteLongwallMaterialUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallMaterialUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallMaterialUnitPriceList")]
    [OpenApiOperation("Delete Multiple Longwall MaterialUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaterialunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> DeleteLongwallMaterialUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLongwallMaterialUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallMaterialUnitPrice/export")]
    [OpenApiOperation("Export LongwallMaterialUnitPrice", "")]
    [HasPermission("pricing.longwallmaterialunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> ExportLongwallMaterialUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallMaterialUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Lo_cho_don_gia_dinh_muc.xlsx");
        return result;
    }

    [HttpPost("LongwallMaterialUnitPrice/import")]
    [OpenApiOperation("Import LongwallMaterialUnitPrice", "")]
    [HasPermission("pricing.longwallmaterialunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức vật liệu (lò chợ)")]

    public async Task<IActionResult> ImportLongwallMaterialUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallMaterialUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    #endregion

    #region SlideUnitPrice

    [HttpGet("SlideUnitPrice")]
    [OpenApiOperation("Get All SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]
    public async Task<IActionResult> GetAllSlideUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSlideUnitPriceQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SlideUnitPrice/Details")]
    [OpenApiOperation("Get All SlideUnitPrice Detail list", "")]
    [HasPermission("pricing.slideunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> GetAllSlideUnitPriceAssignmentCode([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllSlideUnitPriceAssignmentCodeQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("SlideUnitPrice/{id:guid}")]
    [OpenApiOperation("Get SlideUnitPrice By Id", "")]
    [HasPermission("pricing.slideunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> GetSlideUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetSlideUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("SlideUnitPrice")]
    [OpenApiOperation("Update SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> UpdateSlideUnitPrice([FromBody] UopdateSlideUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateSlideUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("SlideUnitPrice")]
    [OpenApiOperation("Create New SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> CreateSlideUnitPrice([FromBody] CreateSlideUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateSlideUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("SlideUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> DeleteSlideUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteSlideUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("SlideUnitPrice")]
    [OpenApiOperation("Delete SlideUnitPrice List", "")]
    [HasPermission("pricing.slideunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> DeleteSlideUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteSlideUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("SlideUnitPrice/export")]
    [OpenApiOperation("Export SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> ExportSlideUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelSlideUnitPriceQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dao_lo_don_gia_mang_truot.xlsx");
        return result;
    }

    [HttpPost("SlideUnitPrice/import")]
    [OpenApiOperation("Import SlideUnitPrice", "")]
    [HasPermission("pricing.slideunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức máng trượt (Đào lò)")]

    public async Task<IActionResult> ImportSlideUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportSlideUnitPriceExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    // check lại cả fe và be 
    #region MaintainUnitPrice

    // Endpoint dùng chung - trả về tất cả loại lò khi maintainType = null
    // Sử dụng bởi form Doanh thu SCTX kế hoạch để load danh sách tổng hợp rồi lọc theo ProcessGroup ở FE
    [HttpGet("MaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All MaintainUnitPriceEquipment (Shared - All Types)", "")]
    public async Task<IActionResult> GetAllMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] MaintainUnitPriceType? maintainType = null)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, maintainType));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("TunnelMaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All Tunnel MaintainUnitPriceEquipment", "")]
    [HasPermission("pricing.maintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> GetAllTunnelMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, MaintainUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TunnelMaintainUnitPriceEquipment")]
    [OpenApiOperation("Create New Tunnel MaintainUnitPriceEquipment", "")]
    [HasPermission("pricing.maintainunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> CreateTunnelMaintainUnitPriceEquipment([FromBody] IList<Application.Dto.Catalog.MaintainUnitPrice.CreateMaintainUnitPriceEquipmentDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = MaintainUnitPriceType.TunnelExcavation;
        }
        var result = await Mediator.Send(new CreateMaintainUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("TunnelMaintainUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Tunnel MaintainUnitPriceEquipment By Equipment Id", "")]
    [HasPermission("pricing.maintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> GetTunnelMaintainUnitPriceEquipmentById([FromRoute] Guid id)
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

    [HttpPost("MaintainUnitPriceEquipment/parts/equipments")]
    [OpenApiOperation("Get Equipments By Part Ids", "")]
    public async Task<IActionResult> GetEquipmentsByPartIds(
        [FromBody] IList<Guid> partIds,
        [FromQuery] DateTime? date = null)
    {
        var result = await Mediator.Send(new GetEquipmentsByPartIdsQuery(partIds, date));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("MaintainUnitPriceEquipment/parts/maintain-unit-price-equipments")]
    [OpenApiOperation("Get MaintainUnitPriceEquipment Ids By Part Ids", "")]
    public async Task<IActionResult> GetMaintainUnitPriceEquipmentsByPartIds(
        [FromBody] IList<Guid> partIds)
    {
        var result = await Mediator.Send(new GetMaintainUnitPriceEquipmentsByPartIdsQuery(partIds));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelMaintainUnitPriceEquipment")]
    [OpenApiOperation("Update Tunnel MaintainUnitPriceEquipment", "")]
    [HasPermission("pricing.maintainunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> UpdateTunnelMaintainUnitPriceEquipment([FromBody] UpdateMaintainUnitPriceDto updateModel)
    {
        updateModel.Type = MaintainUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateMaintainUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TunnelMaintainUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel MaintainUnitPriceEquipment", "")]
    [HasPermission("pricing.maintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> DeleteTunnelMaintainUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("TunnelMaintainUnitPriceEquipment")]
    [OpenApiOperation("Delete Tunnel MaintainUnitPriceEquipment List", "")]
    [HasPermission("pricing.maintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]
    public async Task<IActionResult> DeleteTunnelMaintainUnitPriceEquipmenteList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Tunnel MaintainUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.maintainunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]

    public async Task<IActionResult> ExportTunnelMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_dao_lo.xlsx");
        return result;
    }

    [HttpPost("TunnelMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Tunnel MaintainUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.maintainunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Đào lò)")]

    public async Task<IActionResult> ImportTunnelMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> GetAllTrimmingMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, MaintainUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Create New Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

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
    [HasPermission("pricing.trimmingmaintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> GetTrimmingMaintainUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaintainUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Update Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> UpdateTrimmingMaintainUnitPriceEquipment([FromBody] UpdateMaintainUnitPriceDto updateModel)
    {
        updateModel.Type = MaintainUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateMaintainUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TrimmingMaintainUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> DeleteTrimmingMaintainUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> ExportTrimmingMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTrimmingMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_xen_lo.xlsx");
        return result;
    }

    [HttpPost("TrimmingMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Trimming MaintainUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]

    public async Task<IActionResult> ImportTrimmingMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTrimmingMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpDelete("TrimmingMaintainUnitPriceEquipment")]
    [OpenApiOperation("Delete Trimming MaintainUnitPriceEquipment List (Xén lò)", "")]
    [HasPermission("pricing.trimmingmaintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Xén lò)")]
    public async Task<IActionResult> DeleteTrimmingMaintainUnitPriceEquipmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallMaintainUnitPriceEquipment")]
    [OpenApiOperation("Get All Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> GetAllLongwallMaintainUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllMaintainUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, MaintainUnitPriceType.Longwall));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallMaintainUnitPriceEquipment")]
    [OpenApiOperation("Create New Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> CreateLongwallMaintainUnitPriceEquipment([FromBody] IList<Application.Dto.Catalog.MaintainUnitPrice.CreateMaintainUnitPriceEquipmentDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = MaintainUnitPriceType.Longwall;
        }
        var result = await Mediator.Send(new CreateMaintainUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("LongwallMaintainUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Longwall MaintainUnitPriceEquipment By Id (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> GetLongwallMaintainUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetMaintainUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallMaintainUnitPriceEquipment")]
    [OpenApiOperation("Update Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> UpdateLongwallMaintainUnitPriceEquipment([FromBody] UpdateMaintainUnitPriceDto updateModel)
    {
        updateModel.Type = MaintainUnitPriceType.Longwall;
        var result = await Mediator.Send(new UpdateMaintainUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallMaintainUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> DeleteLongwallMaintainUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("LongwallMaintainUnitPriceEquipment")]
    [OpenApiOperation("Delete Longwall MaintainUnitPriceEquipment List (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]
    public async Task<IActionResult> DeleteLongwallMaintainUnitPriceEquipmentList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteMaintainUnitPriceEquipmentListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallMaintainUnitPriceEquipment/export")]
    [OpenApiOperation("Export Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]

    public async Task<IActionResult> ExportLongwallMaintainUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallMaintainUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_bao_duong_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallMaintainUnitPriceEquipment/import")]
    [OpenApiOperation("Import Longwall MaintainUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallmaintainunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức SCTX (Lò chợ)")]

    public async Task<IActionResult> ImportLongwallMaintainUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLongwallMaintainUnitPriceEquipmentExcelCommand(importModel.FormFile));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion


    #region TunnelElectricityUnitPriceEquipment (Đào lò)

    [HttpGet("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> GetAllTunnelElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
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
    [HasPermission("pricing.tunnerelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> GetTunnelElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelElectricityUnitPriceEquipmentByIdQuery(id, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> UpdateTunnelElectricityUnitPriceEquipment([FromBody] UpdateElectricityUnitPriceEquipmentDto updateModel)
    {
        updateModel.Type = ElectricityUnitPriceType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TunnelElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel ElectricityUnitPriceEquipment (Đào lò)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> DeleteTunnelElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelElectricityUnitPriceEquipmentCommand(deleteId, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Tunnel ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> ExportTunnelElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelElectricityUnitPriceEquipmentQuery(ElectricityUnitPriceType.TunnelExcavation));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_lo_cho.xlsx");
        return result;
    }

    [HttpPost("TunnelElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Tunnel ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.tunnerelectricityunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Đào lò)")]
    public async Task<IActionResult> ImportTunnelElectricityUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelElectricityUnitPriceEquipmentExcelCommand(importModel.FormFile, ElectricityUnitPriceType.TunnelExcavation));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> GetAllTrimmingElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllTunnelElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
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
    [HasPermission("pricing.trimmingelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> GetTrimmingElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetTunnelElectricityUnitPriceEquipmentByIdQuery(id, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TrimmingElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> UpdateTrimmingElectricityUnitPriceEquipment([FromBody] UpdateElectricityUnitPriceEquipmentDto updateModel)
    {
        updateModel.Type = ElectricityUnitPriceType.Trimming;
        var result = await Mediator.Send(new UpdateElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TrimmingElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> DeleteTrimmingElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteTunnelElectricityUnitPriceEquipmentCommand(deleteId, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TrimmingElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> ExportTrimmingElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelTunnelElectricityUnitPriceEquipmentQuery(ElectricityUnitPriceType.Trimming));
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_xen_lo.xlsx");
        return result;
    }

    [HttpPost("TrimmingElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Trimming ElectricityUnitPriceEquipment (Xén lò)", "")]
    [HasPermission("pricing.trimmingelectricityunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Xén lò)")]
    public async Task<IActionResult> ImportTrimmingElectricityUnitPriceEquipment([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportTunnelElectricityUnitPriceEquipmentExcelCommand(importModel.FormFile, ElectricityUnitPriceType.Trimming));
        return Ok(result, MessageCommon.ImportSuccess);
    }
    #endregion

    #region LongwallElectricityUnitPriceEquipment (Lò chợ)

    [HttpGet("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Get All Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> GetAllLongwallElectricityUnitPriceEquipment([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLongwallElectricityUnitPriceEquipmentQuery(pageIndex, pageSize, search, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Create New Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> CreateLongwallElectricityUnitPriceEquipment([FromBody] IList<CreateLongwallElectricityUnitPriceEquipmentDto> createModel)
    {
        var result = await Mediator.Send(new CreateLongwallElectricityUnitPriceEquipmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("LongwallElectricityUnitPriceEquipment/{id:guid}")]
    [OpenApiOperation("Get Longwall ElectricityUnitPriceEquipment By Id (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> GetLongwallElectricityUnitPriceEquipmentById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLongwallElectricityUnitPriceEquipmentByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallElectricityUnitPriceEquipment")]
    [OpenApiOperation("Update Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> UpdateLongwallElectricityUnitPriceEquipment([FromBody] UpdateLongwallElectricityUnitPriceEquipmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdateLongwallElectricityUnitPriceEquipmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallElectricityUnitPriceEquipment/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> DeleteLongwallElectricityUnitPriceEquipment([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLongwallElectricityUnitPriceEquipmentCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallElectricityUnitPriceEquipment/export")]
    [OpenApiOperation("Export Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
    public async Task<IActionResult> ExportLongwallElectricityUnitPriceEquipment()
    {
        var fileByte = await Mediator.Send(new ExportExcelLongwallElectricityUnitPriceEquipmentQuery());
        var result = File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Dinh_muc_dien_lo_cho.xlsx");
        return result;
    }

    [HttpPost("LongwallElectricityUnitPriceEquipment/import")]
    [OpenApiOperation("Import Longwall ElectricityUnitPriceEquipment (Lò chợ)", "")]
    [HasPermission("pricing.longwallelectricityunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức điện năng (Lò chợ)")]
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

    #region LowValuePerishableSupplyUnitPrice

    [HttpGet("TunnelLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Get All LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> GetAllTunnelLowValuePerishableSupplyUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLowValuePerishableSupplyUnitPriceQuery(pageIndex, pageSize, search, ignorePagination, LowValuePerishableSupplyType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("TunnelLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Create Tunnel LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> CreateTunnelLowValuePerishableSupplyUnitPrice([FromBody] IList<CreateLowValuePerishableSupplyUnitPriceDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = LowValuePerishableSupplyType.TunnelExcavation;
        }

        var result = await Mediator.Send(new CreateLowValuePerishableSupplyUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("TunnelLowValuePerishableSupplyUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Tunnel LowValuePerishableSupplyUnitPrice By Id (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]

    public async Task<IActionResult> GetTunnelLowValuePerishableSupplyUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLowValuePerishableSupplyUnitPriceByIdQuery(id, LowValuePerishableSupplyType.TunnelExcavation));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("TunnelLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Update Tunnel LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> UpdateTunnelLowValuePerishableSupplyUnitPrice([FromBody] UpdateLowValuePerishableSupplyUnitPriceDto updateModel)
    {
        updateModel.Type = LowValuePerishableSupplyType.TunnelExcavation;
        var result = await Mediator.Send(new UpdateLowValuePerishableSupplyUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("TunnelLowValuePerishableSupplyUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Tunnel LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> DeleteTunnelLowValuePerishableSupplyUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLowValuePerishableSupplyUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("TunnelLowValuePerishableSupplyUnitPrice/export")]
    [OpenApiOperation("Export Tunnel LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> ExportTunnelLowValuePerishableSupplyUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelLowValuePerishableSupplyUnitPriceQuery(LowValuePerishableSupplyType.TunnelExcavation));
        return File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Don_gia_vat_tu_mau_hong_dao_lo.xlsx");
    }

    [HttpPost("TunnelLowValuePerishableSupplyUnitPrice/import")]
    [OpenApiOperation("Import Tunnel LowValuePerishableSupplyUnitPrice (Đào lò)", "")]
    [HasPermission("pricing.tunnellowvalueperishablesupplyunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Đào lò)")]
    public async Task<IActionResult> ImportTunnelLowValuePerishableSupplyUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLowValuePerishableSupplyUnitPriceExcelCommand(importModel.FormFile, LowValuePerishableSupplyType.TunnelExcavation));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpGet("LongwallLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Get All LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]  
    public async Task<IActionResult> GetAllLongwallLowValuePerishableSupplyUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllLowValuePerishableSupplyUnitPriceQuery(pageIndex, pageSize, search, ignorePagination, LowValuePerishableSupplyType.Longwall));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("LongwallLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Create Longwall LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.create", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> CreateLongwallLowValuePerishableSupplyUnitPrice([FromBody] IList<CreateLowValuePerishableSupplyUnitPriceDto> createModel)
    {
        foreach (var item in createModel)
        {
            item.Type = LowValuePerishableSupplyType.Longwall;
        }

        var result = await Mediator.Send(new CreateLowValuePerishableSupplyUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpGet("LongwallLowValuePerishableSupplyUnitPrice/{id:guid}")]
    [OpenApiOperation("Get Longwall LowValuePerishableSupplyUnitPrice By Id (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.read", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> GetLongwallLowValuePerishableSupplyUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetLowValuePerishableSupplyUnitPriceByIdQuery(id, LowValuePerishableSupplyType.Longwall));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("LongwallLowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Update Longwall LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.update", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> UpdateLongwallLowValuePerishableSupplyUnitPrice([FromBody] UpdateLowValuePerishableSupplyUnitPriceDto updateModel)
    {
        updateModel.Type = LowValuePerishableSupplyType.Longwall;
        var result = await Mediator.Send(new UpdateLowValuePerishableSupplyUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("LongwallLowValuePerishableSupplyUnitPrice/{deleteId:guid}")]
    [OpenApiOperation("Delete Longwall LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> DeleteLongwallLowValuePerishableSupplyUnitPrice([FromRoute] Guid deleteId)
    {
        var result = await Mediator.Send(new DeleteLowValuePerishableSupplyUnitPriceCommand(deleteId));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("LongwallLowValuePerishableSupplyUnitPrice/export")]
    [OpenApiOperation("Export Longwall LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.export", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> ExportLongwallLowValuePerishableSupplyUnitPrice()
    {
        var fileByte = await Mediator.Send(new ExportExcelLowValuePerishableSupplyUnitPriceQuery(LowValuePerishableSupplyType.Longwall));
        return File(fileByte, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Don_gia_vat_tu_mau_hong_lo_cho.xlsx");
    }

    [HttpPost("LongwallLowValuePerishableSupplyUnitPrice/import")]
    [OpenApiOperation("Import Longwall LowValuePerishableSupplyUnitPrice (Lò chợ)", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.import", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> ImportLongwallLowValuePerishableSupplyUnitPrice([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportLowValuePerishableSupplyUnitPriceExcelCommand(importModel.FormFile, LowValuePerishableSupplyType.Longwall));
        return Ok(result, MessageCommon.ImportSuccess);
    }

    [HttpDelete("LowValuePerishableSupplyUnitPrice")]
    [OpenApiOperation("Delete LowValuePerishableSupplyUnitPrice List", "")]
    [HasPermission("pricing.longwalllowvalueperishablesupplyunitprice.delete", "Đơn giá, định mức", "Đơn giá và định mức vật tư mau hỏng rẻ tiền (Lò chợ)")]
    public async Task<IActionResult> DeleteLowValuePerishableSupplyUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteLowValuePerishableSupplyUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }
    #endregion

    #region ProductUnitPrice

    [HttpGet("ProductUnitPrice")]
    [OpenApiOperation("Get All ProductUnitPrice", "")]
    [HasPermission("production.productunitprice.read", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> GetAllProductUnitPrice([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = "", [FromQuery] bool ignorePagination = false, [FromQuery] ProductUnitPriceScenarioType scenarioType = ProductUnitPriceScenarioType.Plan, [FromQuery] Guid? departmentId = null)
    {
        var result = await Mediator.Send(new GetAllProductUnitPriceQuery(pageIndex, pageSize, search, ignorePagination, scenarioType, departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductUnitPrice/Planned/{id:guid}")]
    [OpenApiOperation("Get ProductUnitPrice By Id", "")]
    [HasPermission("production.productunitprice.read", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> GetPlannedProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("ProductUnitPrice/Planned/Department/{departmentId:guid}")]
    [OpenApiOperation("Get Planned ProductUnitPrice By Department", "")]
    [HasPermission("production.productunitprice.read", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> GetPlannedProductUnitPriceByDepartment([FromRoute] Guid departmentId)
    {
        var result = await Mediator.Send(new GetPlannedProductUnitPriceByDepartmentQuery(departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    //thieesu
    [HttpGet("ProductUnitPrice/Actual/{id:guid}")]
    [OpenApiOperation("Get ProductActualUnitPrice By Id", "")]
    public async Task<IActionResult> GetActualProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetActualProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
  
    [HttpGet("ProductUnitPrice/Adjustment/{id:guid}")]
    [OpenApiOperation("Get ProductAdjustmentUnitPrice By Id", "")]
    [HasPermission("production.productionoutput.read", "Thống kê vận hành", "Vận hành sản xuất")]
    public async Task<IActionResult> GetAdjustmentProductUnitPriceById([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentProductUnitPriceByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    
    [HttpGet("ProductUnitPrice/Adjustment/Department/{departmentId:guid}")]
    [OpenApiOperation("Get Adjustment ProductUnitPrice By Department", "")]
    [HasPermission("production.productionoutput.read", "Thống kê vận hành", "Vận hành sản xuất")]
    public async Task<IActionResult> GetAdjustmentProductUnitPriceByDepartment([FromRoute] Guid departmentId)
    {
        var result = await Mediator.Send(new GetAdjustmentProductUnitPriceByDepartmentQuery(departmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("ProductUnitPrice")]
    [OpenApiOperation("Update ProductUnitPrice", "")]
    [HasPermission("production.productunitprice.update", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> UpdateProductUnitPrice([FromBody] UpdateProductUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateProductUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    
    [HttpPut("ProductUnitPrice/Planned-By-Department")]
    [OpenApiOperation("Update Planned ProductUnitPrice By Department", "")]
    [HasPermission("production.productunitprice.update", "Thống kê vận hành", "Kế hoạch sản xuất")]

    public async Task<IActionResult> UpdatePlannedProductUnitPriceByDepartment([FromBody] UpdatePlannedProductUnitPriceByDepartmentDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedProductUnitPriceByDepartmentCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }
    
    [HttpPut("ProductUnitPrice/Adjustment")]
    [OpenApiOperation("Update Adjustment ProductUnitPrice", "")]
    [HasPermission("production.productionoutput.update", "Thống kê vận hành", "Vận hành sản xuất")]

    public async Task<IActionResult> UpdateAdjustmentProductUnitPrice([FromBody] UpdateAdjustmentProductUnitPriceDto updateModel)
    {
        var result = await Mediator.Send(new UpdateAdjustmentProductUnitPriceCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("ProductUnitPrice")]
    [OpenApiOperation("Create New ProductUnitPrice", "")]
    [HasPermission("production.productunitprice.create", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> CreateProductUnitPrice([FromBody] CreateProductUnitPriceDto createModel)
    {
        var result = await Mediator.Send(new CreateProductUnitPriceCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }
    
    [HttpPost("ProductUnitPrice/Planned-By-Department")]
    [OpenApiOperation("Create Planned ProductUnitPrice By Department", "")]
    [HasPermission("production.productunitprice.create", "Thống kê vận hành", "Kế hoạch sản xuất")]

    public async Task<IActionResult> CreatePlannedProductUnitPriceByDepartment([FromBody] CreatePlannedProductUnitPriceByDepartmentDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedProductUnitPriceByDepartmentCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("ProductUnitPrice")]
    [OpenApiOperation("Delete ProductUnitPrice List", "")]
    [HasPermission("production.productunitprice.delete", "Thống kê vận hành", "Kế hoạch sản xuất")]
    public async Task<IActionResult> DeleteProductUnitPriceList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeleteProductUnitPriceListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpGet("ProductUnitPrice/export-adjustment-electricity-maintain-report")]
    [OpenApiOperation("Export Adjustment Electricity And Maintain Report", "Export Bảng tính đơn giá SCTX và điện năng")]
    [HasPermission("report.productunitprice.export", "Báo cáo", "Bảng tính đơn giá SCTX và điện năng")]
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
    [HasPermission("production.plannedmaterialcost.read", "Thống kê vận hành", "Doanh thu vật liệu kế hoạch ban đầu")]
    public async Task<IActionResult> GetPlannedMaterialCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedMaterialCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedMaterialCost")]
    [OpenApiOperation("Update PlannedMaterialCost", "")]
    [HasPermission("production.plannedmaterialcost.update", "Thống kê vận hành", "Doanh thu vật liệu kế hoạch ban đầu")]
    public async Task<IActionResult> UpdatePlannedMaterialCost([FromBody] UpdatePlannedMaterialCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedMaterialCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedMaterialCost")]
    [OpenApiOperation("Create New PlannedMaterialCost", "")]
    [HasPermission("production.plannedmaterialcost.create", "Thống kê vận hành", "Doanh thu vật liệu kế hoạch ban đầu")]
    public async Task<IActionResult> CreatePlannedMaterialCost([FromBody] CreatePlannedMaterialCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedMaterialCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedMaterialCost")]
    [OpenApiOperation("Delete PlannedMaterialCost List", "")]
    [HasPermission("production.plannedmaterialcost.delete", "Thống kê vận hành", "Doanh thu vật liệu kế hoạch ban đầu")]
    public async Task<IActionResult> DeletePlannedMaterialCostList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePlannedMaterialCostListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region PlannedMaintainCost

    [HttpGet("PlannedMaintainCost/{id:guid}")]
    [OpenApiOperation("Get PlannedMaintainCost By Id", "")]
    [HasPermission("production.plannedmaintaincost.read", "Thống kê vận hành","Doanh thu SCTX kế hoạch ban đầu")]
    public async Task<IActionResult> GetPlannedMaintainCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedMaintainCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedMaintainCost")]
    [OpenApiOperation("Update PlannedMaintainCost", "")]
    [HasPermission("production.plannedmaintaincost.update", "Thống kê vận hành", "Doanh thu SCTX kế hoạch ban đầu")]

    public async Task<IActionResult> UpdatePlannedMaintainCost([FromBody] UpdatePlannedMaintainCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedMaintainCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedMaintainCost")]
    [OpenApiOperation("Create New PlannedMaintainCost", "")]
    [HasPermission("production.plannedmaintaincost.create", "Thống kê vận hành", "Doanh thu SCTX kế hoạch ban đầu")]

    public async Task<IActionResult> CreatePlannedMaintainCost([FromBody] CreatePlannedMaintainCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedMaintainCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedMaintainCost")]
    [OpenApiOperation("Delete PlannedMaintainCost List", "")]
    [HasPermission("production.plannedmaintaincost.delete", "Thống kê vận hành", "Doanh thu SCTX kế hoạch ban đầu")]

    public async Task<IActionResult> DeletePlannedMaintainCostList([FromBody] IList<Guid> deleteIds)
    {
        var result = await Mediator.Send(new DeletePlannedMaintainCostListCommand(deleteIds));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion

    #region PlannedElectricityCost
    [HttpGet("PlannedElectricityCost/{id:guid}")]
    [OpenApiOperation("Get PlannedElectricityCost By Id", "")]
    [HasPermission("production.plannedelecticitycost.read", "Thống kê vận hành", "Doanh thu điện năng kế hoạch ban đầu")]

    public async Task<IActionResult> GetPlannedElectricityCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPlannedElectricityCostByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPut("PlannedElectricityCost")]
    [OpenApiOperation("Update PlannedElectricityCost", "")]
    [HasPermission("production.plannedelecticitycost.update", "Thống kê vận hành", "Doanh thu điện năng kế hoạch ban đầu")]

    public async Task<IActionResult> UpdatePlannedElectricityCost([FromBody] UpdatePlannedElectricityCostDto updateModel)
    {
        var result = await Mediator.Send(new UpdatePlannedElectricityCostCommand(updateModel));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("PlannedElectricityCost")]
    [OpenApiOperation("Create New PlannedElectricityCost", "")]
    [HasPermission("production.plannedelecticitycost.create", "Thống kê vận hành", "Doanh thu điện năng kế hoạch ban đầu")]

    public async Task<IActionResult> CreatePlannedElectricityCost([FromBody] CreatePlannedElectricityCostDto createModel)
    {
        var result = await Mediator.Send(new CreatePlannedElectricityCostCommand(createModel));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpDelete("PlannedElectricityCost")]
    [OpenApiOperation("Delete PlannedElectricityCost List", "")]
    [HasPermission("production.plannedelecticitycost.delete", "Thống kê vận hành", "Doanh thu điện năng kế hoạch ban đầu")]

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
    [HasPermission("production.adjustmentmaterialcost.read", "Thống kê vận hành","Doanh thu vật liệu điều chỉnh")]
    public async Task<IActionResult> GetAdjustmnetMaterialCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentMaterialCostByOutputQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentMaintainCost/{id:guid}")]
    [OpenApiOperation("Get AdjustmentMaintainCost By Id", "")]
    [HasPermission("production.adjustmentmaintaincost.read", "Thống kê vận hành", "Doanh thu SCTX điều chỉnh")]

    public async Task<IActionResult> GetAdjustmentMaintainCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentMaintainCostByOutputIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("AdjustmentElectricityCost/{id:guid}")]
    [OpenApiOperation("Get AdjustmentElectricityCost By Id", "")]
    [HasPermission("production.adjustmentelectricitycost.read", "Thống kê vận hành", "Doanh thu điện năng điều chỉnh")]

    public async Task<IActionResult> GetAdjustmentElectricityCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetAdjustmentElectricityCostByOutputIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    #endregion

    #region LumpSumFinalSettlement

    [HttpPost("lump-sum-final-settlement/list")]
    [OpenApiOperation("Get Lump Sum Final Settlement List", "")]
    [HasPermission("production.lumpsumfinalsettlement.read", "Thống kê vận hành","Quyết toán giao khoán")]
    public async Task<IActionResult> GetLumpSumFinalSettlementList([FromBody] LumpSumFinalSettlementListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumFinalSettlementListQuery(request.Month, request.Year, request.ProcessGroupId, request.DepartmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("lump-sum-final-settlement/month-export")]
    [OpenApiOperation("Export Lump Sum Final Settlement Month Excel", "")]
    [HasPermission("report.lumpsumfinalsettlement.export", "Báo cáo", "Bảng thanh toán")]

    public async Task<IActionResult> ExportLumpSumFinalSettlementMonthExcel(
        [FromQuery] string month,
        [FromQuery] string year,
        [FromQuery] string? processGroupId,
        [FromQuery] string? departmentId,
        [FromQuery] string? search)
    {
        var result = await Mediator.Send(new ExportLumpSumFinalSettlementMonthExcelQuery(month, year, processGroupId, departmentId, search));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    [HttpPost("lump-sum-final-settlement/quarter-list")]
    [OpenApiOperation("Get Lump Sum Final Settlement Quarter List", "")]
    [HasPermission("production.lumpsumfinalsettlement.read", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> GetLumpSumFinalSettlementQuarterList([FromBody] LumpSumFinalSettlementQuarterListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumFinalSettlementQuarterListQuery(request.Quarter, request.Year, request.ProcessGroupId, request.DepartmentId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }
    
    [HttpPost("lump-sum-final-settlement/quarter-custom-cost/list")]
    [OpenApiOperation("Get Lump Sum Quarter Custom Cost List", "")]
    [HasPermission("production.lumpsumfinalsettlement.read", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> GetLumpSumQuarterCustomCostList([FromBody] LumpSumQuarterCustomCostListRequest request)
    {
        var result = await Mediator.Send(new GetLumpSumQuarterCustomCostListQuery(request.Quarter, request.Year, request.ProcessGroupId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("lump-sum-final-settlement/quarter-export")]
    [OpenApiOperation("Export Lump Sum Final Settlement Quarter Excel", "")]
    [HasPermission("report.lumpsumfinalsettlement.export", "Báo cáo", "Bảng quyết toán")]

    public async Task<IActionResult> ExportLumpSumFinalSettlementQuarterExcel(
        [FromQuery] string quarter,
        [FromQuery] string year,
        [FromQuery] string? processGroupId,
        [FromQuery] string? departmentId,
        [FromQuery] string? search)
    {
        var result = await Mediator.Send(new ExportLumpSumFinalSettlementQuarterExcelQuery(quarter, year, processGroupId, departmentId, search));
        return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    [HttpPost("lump-sum-final-settlement/quarter-custom-cost")]
    [OpenApiOperation("Create Lump Sum Quarter Custom Cost", "")]
    [HasPermission("production.lumpsumfinalsettlement.create", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> CreateLumpSumQuarterCustomCost([FromBody] CreateLumpSumQuarterCustomCostRequest request)
    {
        var result = await Mediator.Send(new CreateLumpSumQuarterCustomCostCommand(request));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("lump-sum-final-settlement/quarter-custom-cost")]
    [OpenApiOperation("Update Lump Sum Quarter Custom Cost", "")]
    [HasPermission("production.lumpsumfinalsettlement.update", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> UpdateLumpSumQuarterCustomCost([FromBody] UpdateLumpSumQuarterCustomCostRequest request)
    {
        var result = await Mediator.Send(new UpdateLumpSumQuarterCustomCostCommand(request));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPut("lump-sum-final-settlement/month-special-quantity")]
    [OpenApiOperation("Update Lump Sum Month Special Quantity", "")]
    [HasPermission("production.lumpsumfinalsettlement.update", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> UpdateLumpSumMonthSpecialQuantity([FromBody] UpdateLumpSumMonthSpecialQuantityRequest request)
    {
        var result = await Mediator.Send(new UpdateLumpSumMonthSpecialQuantityCommand(request));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPut("lump-sum-final-settlement/month-carry-forward")]
    [OpenApiOperation("Update Lump Sum Month Carry Forward Value", "")]
    [HasPermission("production.lumpsumfinalsettlement.update", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> UpdateLumpSumMonthCarryForward([FromBody] UpdateLumpSumMonthCarryForwardRequest request)
    {
        var result = await Mediator.Send(new UpdateLumpSumMonthCarryForwardCommand(request));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("lump-sum-final-settlement/quarter-custom-cost/{id:guid}")]
    [OpenApiOperation("Delete Lump Sum Quarter Custom Cost", "")]
    [HasPermission("production.lumpsumfinalsettlement.delete", "Thống kê vận hành", "Quyết toán giao khoán")]

    public async Task<IActionResult> DeleteLumpSumQuarterCustomCost([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new DeleteLumpSumQuarterCustomCostCommand(id));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    #endregion
}


