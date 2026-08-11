using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Queries;

public record ExportExcelScaniaTruckUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelScaniaTruckUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelScaniaTruckUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<HaulDistance> _haulDistanceRepository = unitOfWork.GetRepository<HaulDistance>();
    private readonly IWriteRepository<CargoType> _cargoTypeRepository = unitOfWork.GetRepository<CargoType>();
    private readonly IWriteRepository<TransportLocation> _transportLocationRepository = unitOfWork.GetRepository<TransportLocation>();

    public async Task<byte[]> Handle(ExportExcelScaniaTruckUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(ScaniaTruckUnitPriceExcelDto.HeaderId)];

        var list = await _repository.GetAllAsync(
            include: q => q
                .Include(s => s.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(s => s.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(s => s.CargoType).ThenInclude(c => c!.Code)
                .Include(s => s.ReceivingLocation).ThenInclude(r => r!.Code)
                .Include(s => s.DumpingLocation).ThenInclude(d => d!.Code)
                .Include(s => s.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true);

        var assignmentCodeOptions = (await _assignmentCodeRepository.GetAllAsync(
                include: q => q.Include(a => a.Code),
                selector: a => a.Code != null ? a.Code.Value + " - " + a.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var productionProcessOptions = (await _productionProcessRepository.GetAllAsync(
                predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTCG,
                include: q => q.Include(p => p.Code),
                selector: p => p.Code != null ? p.Code.Value + " - " + p.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var cargoTypeOptions = (await _cargoTypeRepository.GetAllAsync(
                include: q => q.Include(c => c.Code),
                selector: c => c.Code != null ? c.Code.Value + " - " + c.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var receivingLocationOptions = (await _transportLocationRepository.GetAllAsync(
                predicate: t => t.LocationType == LocationType.Receiving || t.LocationType == LocationType.Both,
                include: q => q.Include(t => t.Code),
                selector: t => t.Code != null ? t.Code.Value + " - " + t.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var dumpingLocationOptions = (await _transportLocationRepository.GetAllAsync(
                predicate: t => t.LocationType == LocationType.Dumping || t.LocationType == LocationType.Both,
                include: q => q.Include(t => t.Code),
                selector: t => t.Code != null ? t.Code.Value + " - " + t.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var haulDistanceOptions = (await _haulDistanceRepository.GetAllAsync(
                selector: h => h.Value,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(ScaniaTruckUnitPriceExcelDto.AssignmentCodeCode), assignmentCodeOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.ProductionProcessCode), productionProcessOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.CargoTypeCode), cargoTypeOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.ReceivingLocationCode), receivingLocationOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.DumpingLocationCode), dumpingLocationOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.EquipmentQuality), new List<string> { "A", "B", "C" } },
            { nameof(ScaniaTruckUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        var dtoList = new List<ScaniaTruckUnitPriceExcelDto>();
        foreach (var header in list)
        {
            var assignmentCodeText = header.AssignmentCode?.Code != null
                ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}" : string.Empty;
            var productionProcessText = header.ProductionProcess?.Code != null
                ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}" : string.Empty;
            var cargoTypeText = header.CargoType?.Code != null
                ? $"{header.CargoType.Code.Value} - {header.CargoType.Name}" : string.Empty;
            var receivingText = header.ReceivingLocation?.Code != null
                ? $"{header.ReceivingLocation.Code.Value} - {header.ReceivingLocation.Name}" : null;
            var dumpingText = header.DumpingLocation?.Code != null
                ? $"{header.DumpingLocation.Code.Value} - {header.DumpingLocation.Name}" : null;

            foreach (var detail in header.Details)
            {
                dtoList.Add(new ScaniaTruckUnitPriceExcelDto
                {
                    HeaderId = header.Id,
                    StartMonth = header.StartMonth.ToString("MM/yyyy"),
                    EndMonth = header.EndMonth.ToString("MM/yyyy"),
                    AssignmentCodeCode = assignmentCodeText,
                    EquipmentQuality = header.EquipmentQuality,
                    ProductionProcessCode = productionProcessText,
                    CargoTypeCode = cargoTypeText,
                    ReceivingLocationCode = receivingText,
                    DumpingLocationCode = dumpingText,
                    HaulDistanceValue = detail.HaulDistance?.Value ?? string.Empty,
                    FuelUnitPrice = detail.FuelUnitPrice,
                    PowerUnitPrice = detail.PowerUnitPrice ?? 0,
                    MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                });
            }
        }

        return excelService.ExportToExcel(dtoList, "Xe Scania", hiddenProperties, dropdownConfigs);
    }
}