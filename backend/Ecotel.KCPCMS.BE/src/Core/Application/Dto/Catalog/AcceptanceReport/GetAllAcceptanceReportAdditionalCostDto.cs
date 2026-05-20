namespace Application.Dto.Catalog.AcceptanceReport;

public record AdditionalCostItemDto
{
    public Guid? MaterialId { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? UnitOfMeasureName { get; init; }
    public double AdditionalCostQuantity { get; init; }
}

public record AdditionalCostsGroupDto
{
    public List<AdditionalCostItemDto> Material { get; init; } = new();
    public List<AdditionalCostItemDto> Maintain { get; init; } = new();
    public List<AdditionalCostItemDto> OtherMaterial { get; init; } = new();
}

public record GetAllAcceptanceReportAdditionalCostResponseDto
{
    public required Guid Id { get; init; }
    public required AdditionalCostsGroupDto AdditionalCosts { get; init; }
}
