using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;

namespace Infrastructure.Services.Catalog;

public class CostService(IUnitOfWork unitOfWork) : ICostService
{
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();

    public string BuildExcelCostString(IList<Cost> costs)
    {
        var costStrings = costs.Select(c => $"{c.StartMonth:MM/yyyy}~{c.EndMonth:MM/yyyy}={c.Amount}&{c.ActualAmount}");
        return string.Join("; ", costStrings);
    }

    public string BuildExcelActualCostString(IList<Cost> costs)
    {
        var costStrings = costs.Select(c => $"{c.StartMonth:MM/yyyy}~{c.EndMonth:MM/yyyy}={c.ActualAmount}");
        return string.Join("; ", costStrings);
    }

    public List<Cost> ParseExcelCostString(string costString, CostType type, Guid costTypeId)
    {
        var result = new List<Cost>();

        if (string.IsNullOrWhiteSpace(costString))
        {
            return result;
        }

        var records = costString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var record in records)
        {
            var parts = record.Split('=');
            if (parts.Length != 2)
            {
                continue;
            }

            var amount = parts[1].Trim().Split('&');
            var planCost = decimal.Parse(amount[0]);
            var actualCost = decimal.Parse(amount[1]);

            var dateParts = parts[0].Trim().Split('~');
            if (dateParts.Length != 2)
            {
                continue;
            }

            var startDate = DateOnly.ParseExact(dateParts[0].Trim(), "MM/yyyy", null);
            var endDate = DateOnly.ParseExact(dateParts[1].Trim(), "MM/yyyy", null);

            result.Add(Cost.Create(startDate, endDate, type, (double)planCost, costTypeId, (double)actualCost));
        }

        return result;
    }

    public async Task<bool> IsOverlap(DateOnly startMonth, DateOnly endMonth, CostType costType)
    {
        return await _costRepository.AnyAsync(c =>
            c.CostType == costType && c.StartMonth < endMonth && c.EndMonth > startMonth);
    }

    public async Task<bool> IsOverlap(IList<Cost> costs)
    {
        var ordered = costs.OrderBy(c => c.StartMonth).ThenBy(c => c.EndMonth).ToList();
        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].StartMonth < ordered[i - 1].EndMonth)
            {
                return true; // overlap nội bộ
            }
        }
        foreach (var cost in costs)
        {
            bool conflict = await _costRepository.AnyAsync(c =>
                c.CostType == cost.CostType
                && c.AssignmentCodeId == cost.AssignmentCodeId
                && c.PartId == cost.PartId
                && c.MaterialId == cost.MaterialId
                && c.EquipmentId == cost.EquipmentId
                && c.StartMonth < cost.EndMonth
                && c.EndMonth > cost.StartMonth
                && c.DeletedOn == null);

            if (conflict)
            {
                return true; // conflict với DB
            }
        }

        return false;
    }

    public bool AreCostsChanged(IList<Cost> dbCosts, IList<Cost> excelCosts)
    {
        if ((dbCosts == null || !dbCosts.Any()) && (excelCosts == null || !excelCosts.Any()))
        {
            return false;
        }

        // Build chuỗi từ cả 2 danh sách và so sánh
        string dbString = BuildExcelCostString(dbCosts);
        string excelString = BuildExcelCostString(excelCosts);
        return dbString != excelString;
    }
}
