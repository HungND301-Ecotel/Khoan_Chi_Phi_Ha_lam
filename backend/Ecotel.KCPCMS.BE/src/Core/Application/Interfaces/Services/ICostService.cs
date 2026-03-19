using Domain.Common.Enums;
using Domain.Entities.Index;

namespace Application.Interfaces.Services;

public interface ICostService
{
    public Task<bool> IsOverlap(DateOnly startMonth, DateOnly endMonth, CostType costType);
    public Task<bool> IsOverlap(IList<Cost> costs);
    public string BuildExcelCostString(IList<Cost> costs);
    public List<Cost> ParseExcelCostString(string costString, CostType type, Guid costTypeId);

    public bool AreCostsChanged(IList<Cost> dbCosts, IList<Cost> excelCosts);
}
