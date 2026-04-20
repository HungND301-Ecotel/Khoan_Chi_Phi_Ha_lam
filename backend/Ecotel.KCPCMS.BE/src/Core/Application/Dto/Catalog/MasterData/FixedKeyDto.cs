using Application.Common.Interfaces;
using Domain.Common.Enums;

namespace Application.Dto.Catalog.MasterData;

public class FixedKeyDto : IDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public FixedKeyType Type { get; set; }
    public bool IsSystem { get; set; }
}

public class CreateFixedKeyDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public FixedKeyType Type { get; set; }
    public bool IsSystem { get; set; } = true;
}