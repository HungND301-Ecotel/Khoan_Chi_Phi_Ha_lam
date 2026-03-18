using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record ExportExcelLongwallElectricityUnitPriceEquipmentQuery() : IRequest<byte[]>;

public class ExportExcelLongwallElectricityUnitPriceEquipmentQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelLongwallElectricityUnitPriceEquipmentQuery, byte[]>
{
    private readonly IWriteRepository<LongwallElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<LongwallElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<byte[]> Handle(ExportExcelLongwallElectricityUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(LongwallElectricityUnitPriceEquipmentExcelDto.Id));

        var list = await _repository.GetAllAsync(
            include: e => e
                .Include(e => e.Equipment)
                    .ThenInclude(eq => eq!.Code)
                .Include(e => e.Equipment)
                    .ThenInclude(eq => eq!.UnitOfMeasure),
            disableTracking: true);

        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e.Include(e => e.Code),
            selector: e => e.Code != null ? e.Code.Value : "",
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(LongwallElectricityUnitPriceEquipmentExcelDto.EquipmentCode), equipments.Where(c => !string.IsNullOrEmpty(c)).ToList() }
        };

        var dtoList = list.Select(e => new LongwallElectricityUnitPriceEquipmentExcelDto
        {
            Id = e.Id,
            EquipmentCode = e.Equipment?.Code?.Value ?? "",
            UnitOfMeasureName = e.Equipment?.UnitOfMeasure?.Name ?? "",
            Quantity = e.Quantity,
            Pdm = e.Pdm,
            Kyc = e.Kyc,
            Kdt = e.Kdt,
            WorkingHour = e.WorkingHour,
            WorkingDate = e.WorkingDate,
            AverageMonthlyTunnelProduction = e.AverageMonthlyTunnelProduction,
            StartMonth = e.StartMonth.ToString("MM/yyyy"),
            EndMonth = e.EndMonth.ToString("MM/yyyy")
        });

        return excelService.ExportToExcel(
            data: dtoList,
            sheetName: "Định mức điện lò chợ",
            hiddenProperties: listHiddenProperty,
            dropdownData: dropdownConfigs);
    }
}
