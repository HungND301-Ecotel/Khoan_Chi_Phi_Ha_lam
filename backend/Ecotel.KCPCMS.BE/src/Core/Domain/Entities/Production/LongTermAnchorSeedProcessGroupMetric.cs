using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class LongTermAnchorSeedProcessGroupMetric : AuditableEntity<Guid>
{
    public Guid LongTermAnchorSeedId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public double PlannedOutput { get; protected set; }
    public double StandardOutput { get; protected set; }

    public virtual LongTermAnchorSeed LongTermAnchorSeed { get; protected set; } = default!;
    public virtual ProcessGroup ProcessGroup { get; protected set; } = default!;

    public static LongTermAnchorSeedProcessGroupMetric Create(
        Guid longTermAnchorSeedId,
        Guid processGroupId,
        double plannedOutput,
        double standardOutput)
    {
        Validate(plannedOutput, standardOutput);

        return new LongTermAnchorSeedProcessGroupMetric
        {
            LongTermAnchorSeedId = longTermAnchorSeedId,
            ProcessGroupId = processGroupId,
            PlannedOutput = plannedOutput,
            StandardOutput = standardOutput
        };
    }

    public void Update(double plannedOutput, double standardOutput)
    {
        Validate(plannedOutput, standardOutput);

        PlannedOutput = plannedOutput;
        StandardOutput = standardOutput;
    }

    private static void Validate(double plannedOutput, double standardOutput)
    {
        if (plannedOutput < 0)
        {
            throw new ArgumentException("Sản lượng kế hoạch không được âm");
        }

        if (standardOutput < 0)
        {
            throw new ArgumentException("Sản lượng định mức không được âm");
        }
    }
}
