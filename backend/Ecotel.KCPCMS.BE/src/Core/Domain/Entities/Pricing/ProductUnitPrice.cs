using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing;

public class ProductUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProductId { get; protected set; }
    public Guid? UnitOfMeasureId { get; protected set; }
    public Guid? DepartmentId { get; protected set; }
    public ProductUnitPriceScenarioType ScenarioType { get; protected set; } = ProductUnitPriceScenarioType.Plan;

    //Navigation properties
    public Product? Product { get; protected set; }
    public UnitOfMeasure? UnitOfMeasure { get; protected set; }
    public Department? Department { get; protected set; }

    private IList<Output> _outputs = new List<Output>();
    public virtual IReadOnlyCollection<Output> Outputs => _outputs.AsReadOnly();

    private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
    public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

    private IList<PlannedMaintainCost> _plannedMaintainCosts = new List<PlannedMaintainCost>();
    public virtual IReadOnlyCollection<PlannedMaintainCost> PlannedMaintainCosts => _plannedMaintainCosts.AsReadOnly();

    private IList<PlannedElectricityCost> _plannedElectricityCosts = new List<PlannedElectricityCost>();
    public virtual IReadOnlyCollection<PlannedElectricityCost> PlannedElectricityCosts => _plannedElectricityCosts.AsReadOnly();

    private IList<ProductUnitPriceProductionOutput> _productUnitPriceProductionOutputs = new List<ProductUnitPriceProductionOutput>();
    public virtual IReadOnlyCollection<ProductUnitPriceProductionOutput> ProductUnitPriceProductionOutputs => _productUnitPriceProductionOutputs.AsReadOnly();

    //Constructor
    public static ProductUnitPrice Create(Guid productId, Guid? unitOfMeasureId, Guid? departmentId, ProductUnitPriceScenarioType scenarioType = ProductUnitPriceScenarioType.Plan)
    {
        return new ProductUnitPrice
        {
            ProductId = productId,
            UnitOfMeasureId = unitOfMeasureId,
            DepartmentId = departmentId,
            ScenarioType = scenarioType
        };
    }

    public void Update(Guid productId, Guid? unitOfMeasureId, Guid? departmentId)
    {
        ProductId = productId;
        UnitOfMeasureId = unitOfMeasureId;
        DepartmentId = departmentId;
    }

    public void AddProductionOutput(Guid productionOutputId, double productionMeters = 0)
    {
        var exists = _productUnitPriceProductionOutputs.Any(p => p.ProductionOutputId == productionOutputId);
        if (!exists)
        {
            var link = ProductUnitPriceProductionOutput.Create(Id, productionOutputId, productionMeters);
            _productUnitPriceProductionOutputs.Add(link);
        }
        else
        {
            var existing = _productUnitPriceProductionOutputs.First(p => p.ProductionOutputId == productionOutputId);
            existing.UpdateProductionMeters(productionMeters);
        }
    }

    public void RemoveProductionOutput(Guid productionOutputId)
    {
        var link = _productUnitPriceProductionOutputs.FirstOrDefault(p => p.ProductionOutputId == productionOutputId);
        if (link != null)
        {
            _productUnitPriceProductionOutputs.Remove(link);
        }
    }

    public void ClearProductionOutputs()
    {
        _productUnitPriceProductionOutputs.Clear();
    }

    public void AddOutputs(IEnumerable<Output> outputs)
    {
        foreach (var output in outputs)
        {
            output.Update(output.ProductionMeters, output.StartMonth, output.EndMonth);
            _outputs.Add(output);
        }
    }
    public void AddOutput(Output output)
    {
        output.Update(output.ProductionMeters, output.StartMonth, output.EndMonth);
        _outputs.Add(output);
    }

    public void ClearOutputs()
    {
        _outputs.Clear();
    }

    public double GetPlannedTotalPrice()
    {
        if (!Outputs.Any()) { return 0; }
        return Outputs
            .Where(o => o.OutputType == OutputType.PlanOutput)
            .Sum(o => o.GetPlannedTotalPrice());
    }

    public double GetActualTotalPrice()
    {
        if (!Outputs.Any()) { return 0; }
        return Outputs
            .Where(o => o.OutputType == OutputType.ActualOutput)
            .Sum(o => o.GetActualTotalPrice());
    }

    public double GetAdjustmentTotalPrice()
    {
        if (!Outputs.Any()) { return 0; }

        var plannedOutputsDict = Outputs
            .Where(o => o.OutputType == OutputType.PlanOutput)
            .ToDictionary(o => (o.StartMonth, o.EndMonth), o => o);

        return Outputs
            .Where(o => o.OutputType == OutputType.ActualOutput)
            .Sum(actualOutput =>
            {
                if (plannedOutputsDict.TryGetValue((actualOutput.StartMonth, actualOutput.EndMonth), out var plannedOutput))
                {
                    return plannedOutput.GetAdjustmentTotalPrice(actualOutput.ProductionMeters);
                }
                return 0;
            });
    }
}
