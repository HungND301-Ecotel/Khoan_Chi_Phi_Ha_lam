using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;

namespace Application.Dto.Catalog.AdjustmentFactorDescription;

public class AdjustmentFactorDescriptionDto : IDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public Guid AdjustmentFactorId { get; set; }
    public string AdjustmentFactorCode { get; set; } = "";
    public double? MaintenanceAdjustmentValue { get; set; }
    public double? ElectricityAdjustmentValue { get; set; }
}

public class AdjustmentFactorDescriptionExcelDto
{
    public Guid Id { get; set; }
    [Display(Name = "Mã HSĐC")]
    public string AdjustmentFactorCode { get; set; } = "";
    [Display(Name = "Diễn giải HSĐC")]
    public string Description { get; set; } = "";
    [Display(Name = "Trị số điều chỉnh SCTX")]
    public double? MaintenanceAdjustmentValue { get; set; }
    [Display(Name = "Trị số điều chỉnh điện năng")]
    public double? ElectricityAdjustmentValue { get; set; }
}

public class ShortAdjustmentFactorDescriptionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public double? MaintenanceAdjustmentValue { get; set; }
    public double? ElectricityAdjustmentValue { get; set; }
}

public class MaintainAjustmentFactorDescriptionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public Guid AdjustmentFactorId { get; set; }
    public string AdjustmentFactorCode { get; set; } = "";
    public string AdjustmentFactorName { get; set; } = "";
    public double? MaintenanceAdjustmentValue { get; set; }
}

public class ElectricityAjustmentFactorDescriptionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public Guid AdjustmentFactorId { get; set; }
    public string AdjustmentFactorCode { get; set; } = "";
    public string AdjustmentFactorName { get; set; } = "";
    public double? ElectricityAdjustmentValue { get; set; }
}

public class CreateAdjustmentFactorDescriptionDto
{
    public string Description { get; set; } = "";
    public Guid AdjustmentFactorId { get; set; }
    public double? MaintenanceAdjustmentValue { get; set; }
    public double? ElectricityAdjustmentValue { get; set; }
}

public class UpdateAdjustmentFactorDescriptionDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public Guid AdjustmentFactorId { get; set; }
    public double? MaintenanceAdjustmentValue { get; set; }
    public double? ElectricityAdjustmentValue { get; set; }
}
