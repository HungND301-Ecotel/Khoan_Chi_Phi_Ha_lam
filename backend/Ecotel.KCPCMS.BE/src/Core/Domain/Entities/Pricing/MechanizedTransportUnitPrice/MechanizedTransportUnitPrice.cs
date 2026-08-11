using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public abstract class MechanizedTransportUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    private static readonly string[] ValidEquipmentQualities = { "A", "B", "C" };

    public Guid AssignmentCodeId { get; protected set; }
    public string EquipmentQuality { get; protected set; }
    public Guid ProductionProcessId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }

    public virtual AssignmentCode? AssignmentCode { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }

    private readonly IList<MechanizedTransportUnitPriceDetail> _details = new List<MechanizedTransportUnitPriceDetail>();
    public IReadOnlyCollection<MechanizedTransportUnitPriceDetail> Details => _details.AsReadOnly();

    protected static void ValidateHeader( Guid assignmentCodeId, string equipmentQuality, Guid productionProcessId, DateOnly startMonth, DateOnly endMonth)
    {
        ValidateHeaderForUpdate(assignmentCodeId, equipmentQuality, productionProcessId, startMonth, endMonth);
    }

    protected static void ValidateHeaderForUpdate(Guid assignmentCodeId, string equipmentQuality, Guid productionProcessId, DateOnly startMonth, DateOnly endMonth)
    {
        if (assignmentCodeId == Guid.Empty)
        {
            throw new ArgumentException("Nhóm vật tư, tài sản không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(equipmentQuality) || !ValidEquipmentQualities.Contains(equipmentQuality.ToUpper()))
        {
            throw new ArgumentException("Chất lượng thiết bị phải là A, B hoặc C.");
        }
        if (productionProcessId == Guid.Empty)
        {
            throw new ArgumentException("Công đoạn sản xuất không được để trống.");
        }
        if (startMonth > endMonth)
        {
            throw new ArgumentException("Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
        }
    }

    protected void SetHeader(Guid assignmentCodeId, string equipmentQuality, Guid productionProcessId, DateOnly startMonth, DateOnly endMonth)
    {
        AssignmentCodeId = assignmentCodeId;
        EquipmentQuality = equipmentQuality.ToUpper();
        ProductionProcessId = productionProcessId;
        StartMonth = startMonth;
        EndMonth = endMonth;
    }

    protected void ReplaceDetails(IEnumerable<MechanizedTransportUnitPriceDetailInput> details, bool allowPower)
    {
        var detailList = details?.ToList() ?? new List<MechanizedTransportUnitPriceDetailInput>();
        if (detailList.Count == 0)
        {
            throw new ArgumentException("Phải nhập ít nhất một dòng đơn giá.");
        }

        _details.Clear();
        foreach (var detail in detailList)
        {
            _details.Add(MechanizedTransportUnitPriceDetail.Create(detail.HaulDistanceId, detail.FuelUnitPrice, allowPower ? detail.PowerUnitPrice : null, detail.MaintenanceUnitPrice));
        }
    }
}