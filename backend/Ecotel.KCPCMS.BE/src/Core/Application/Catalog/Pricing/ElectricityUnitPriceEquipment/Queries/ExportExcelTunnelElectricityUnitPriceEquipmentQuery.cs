using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record ExportExcelTunnelElectricityUnitPriceEquipmentQuery() : IRequest<byte[]>;

public class ExportExcelTunnelElectricityUnitPriceEquipmentQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelTunnelElectricityUnitPriceEquipmentQuery, byte[]>
{
    private readonly IWriteRepository<TunnelElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<TunnelElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<byte[]> Handle(ExportExcelTunnelElectricityUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(TunnelElectricityUnitPriceEquipmentExcelDto.Id));

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
            { nameof(TunnelElectricityUnitPriceEquipmentExcelDto.EquipmentCode), equipments.Where(c => !string.IsNullOrEmpty(c)).ToList() }
        };

        var dtoList = list.Select(e => new TunnelElectricityUnitPriceEquipmentExcelDto
        {
            Id = e.Id,
            EquipmentCode = e.Equipment?.Code?.Value ?? "",
            UnitOfMeasureName = e.Equipment?.UnitOfMeasure?.Name ?? "",
            MonthlyElectricityCost = e.MonthlyElectricityCost,
            AverageMonthlyTunnelProduction = e.AverageMonthlyTunnelProduction,
            StartMonth = e.StartMonth.ToString("MM/yyyy"),
            EndMonth = e.EndMonth.ToString("MM/yyyy")
        });

        return excelService.ExportToExcel(
            data: dtoList,
            sheetName: "Định mức điện lò đào",
            hiddenProperties: listHiddenProperty,
            dropdownData: dropdownConfigs);
    }
}
