using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.Queries;

public record ExportExcelAllMechanizedTransportUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelAllMechanizedTransportUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelAllMechanizedTransportUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<HaulDistance> _haulDistanceRepository = unitOfWork.GetRepository<HaulDistance>();
    private readonly IWriteRepository<CargoType> _cargoTypeRepository = unitOfWork.GetRepository<CargoType>();
    private readonly IWriteRepository<TransportLocation> _transportLocationRepository = unitOfWork.GetRepository<TransportLocation>();

    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _scaniaRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _wasteRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _serviceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _excavatorRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<byte[]> Handle(ExportExcelAllMechanizedTransportUnitPriceQuery request, CancellationToken cancellationToken)
    {
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

        var haulDistanceOptions = (await _haulDistanceRepository.GetAllAsync(
                selector: h => h.Value,
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

        var qualityOptions = new List<string> { "A", "B", "C" };

        using var workbook = new XLWorkbook();

        await AddScaniaSheet(workbook, assignmentCodeOptions, productionProcessOptions, cargoTypeOptions, receivingLocationOptions, dumpingLocationOptions, qualityOptions, haulDistanceOptions);
        await AddWasteSuctionSheet(workbook, assignmentCodeOptions, productionProcessOptions, qualityOptions, haulDistanceOptions);
        await AddServiceAndCraneSheet(workbook, assignmentCodeOptions, productionProcessOptions, qualityOptions, haulDistanceOptions);
        await AddExcavatorSheet(workbook, assignmentCodeOptions, productionProcessOptions, qualityOptions);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task AddScaniaSheet(
        XLWorkbook workbook, List<string> assignmentOptions, List<string> processOptions,
        List<string> cargoTypeOptions, List<string> receivingOptions, List<string> dumpingOptions,
        List<string> qualityOptions, List<string> haulDistanceOptions)
    {
        var list = await _scaniaRepository.GetAllAsync(
            include: q => q
                .Include(s => s.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(s => s.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(s => s.CargoType).ThenInclude(c => c!.Code)
                .Include(s => s.ReceivingLocation).ThenInclude(r => r!.Code)
                .Include(s => s.DumpingLocation).ThenInclude(d => d!.Code)
                .Include(s => s.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true);

        var dtoList = new List<ScaniaTruckUnitPriceExcelDto>();
        foreach (var header in list)
        {
            var assignmentText = header.AssignmentCode?.Code != null ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}" : string.Empty;
            var processText = header.ProductionProcess?.Code != null ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}" : string.Empty;
            var cargoText = header.CargoType?.Code != null ? $"{header.CargoType.Code.Value} - {header.CargoType.Name}" : string.Empty;
            var receivingText = header.ReceivingLocation?.Code != null ? $"{header.ReceivingLocation.Code.Value} - {header.ReceivingLocation.Name}" : null;
            var dumpingText = header.DumpingLocation?.Code != null ? $"{header.DumpingLocation.Code.Value} - {header.DumpingLocation.Name}" : null;

            foreach (var detail in header.Details)
            {
                dtoList.Add(new ScaniaTruckUnitPriceExcelDto
                {
                    HeaderId = header.Id,
                    StartMonth = header.StartMonth.ToString("MM/yyyy"),
                    EndMonth = header.EndMonth.ToString("MM/yyyy"),
                    AssignmentCodeCode = assignmentText,
                    EquipmentQuality = header.EquipmentQuality,
                    ProductionProcessCode = processText,
                    CargoTypeCode = cargoText,
                    ReceivingLocationCode = receivingText,
                    DumpingLocationCode = dumpingText,
                    HaulDistanceValue = detail.HaulDistance?.Value ?? string.Empty,
                    FuelUnitPrice = detail.FuelUnitPrice,
                    PowerUnitPrice = detail.PowerUnitPrice ?? 0,
                    MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                });
            }
        }

        Dictionary<string, List<string>> dropdowns = new()
        {
            { nameof(ScaniaTruckUnitPriceExcelDto.AssignmentCodeCode), assignmentOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.ProductionProcessCode), processOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.CargoTypeCode), cargoTypeOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.ReceivingLocationCode), receivingOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.DumpingLocationCode), dumpingOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.EquipmentQuality), qualityOptions },
            { nameof(ScaniaTruckUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        excelService.AddSheetWithDropdown(workbook, dtoList, "Xe Scania", new List<string> { nameof(ScaniaTruckUnitPriceExcelDto.HeaderId) }, dropdowns);
    }

    private async Task AddWasteSuctionSheet(
        XLWorkbook workbook, List<string> assignmentOptions, List<string> processOptions,
        List<string> qualityOptions, List<string> haulDistanceOptions)
    {
        var list = await _wasteRepository.GetAllAsync(
            include: q => q
                .Include(w => w.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(w => w.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(w => w.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true);

        var dtoList = new List<WasteSuctionTruckUnitPriceExcelDto>();
        foreach (var header in list)
        {
            var assignmentText = header.AssignmentCode?.Code != null ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}" : string.Empty;
            var processText = header.ProductionProcess?.Code != null ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}" : string.Empty;

            foreach (var detail in header.Details)
            {
                dtoList.Add(new WasteSuctionTruckUnitPriceExcelDto
                {
                    HeaderId = header.Id,
                    StartMonth = header.StartMonth.ToString("MM/yyyy"),
                    EndMonth = header.EndMonth.ToString("MM/yyyy"),
                    AssignmentCodeCode = assignmentText,
                    EquipmentQuality = header.EquipmentQuality,
                    ProductionProcessCode = processText,
                    HaulDistanceValue = detail.HaulDistance?.Value,
                    FuelUnitPrice = detail.FuelUnitPrice,
                    MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                });
            }
        }

        Dictionary<string, List<string>> dropdowns = new()
        {
            { nameof(WasteSuctionTruckUnitPriceExcelDto.AssignmentCodeCode), assignmentOptions },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.ProductionProcessCode), processOptions },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.EquipmentQuality), qualityOptions },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        excelService.AddSheetWithDropdown(workbook, dtoList, "Xe hut bun chat thai", new List<string> { nameof(WasteSuctionTruckUnitPriceExcelDto.HeaderId) }, dropdowns);
    }

    private async Task AddServiceAndCraneSheet(
        XLWorkbook workbook, List<string> assignmentOptions, List<string> processOptions,
        List<string> qualityOptions, List<string> haulDistanceOptions)
    {
        var list = await _serviceRepository.GetAllAsync(
            include: q => q
                .Include(s => s.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(s => s.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(s => s.Details).ThenInclude(d => d.HaulDistance),
            disableTracking: true);

        var dtoList = new List<ServiceAndCraneVehicleUnitPriceExcelDto>();
        foreach (var header in list)
        {
            var assignmentText = header.AssignmentCode?.Code != null ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}" : string.Empty;
            var processText = header.ProductionProcess?.Code != null ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}" : string.Empty;

            foreach (var detail in header.Details)
            {
                dtoList.Add(new ServiceAndCraneVehicleUnitPriceExcelDto
                {
                    HeaderId = header.Id,
                    StartMonth = header.StartMonth.ToString("MM/yyyy"),
                    EndMonth = header.EndMonth.ToString("MM/yyyy"),
                    AssignmentCodeCode = assignmentText,
                    EquipmentQuality = header.EquipmentQuality,
                    ProductionProcessCode = processText,
                    HaulDistanceValue = detail.HaulDistance?.Value,
                    FuelUnitPrice = detail.FuelUnitPrice,
                    MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                });
            }
        }

        Dictionary<string, List<string>> dropdowns = new()
        {
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.AssignmentCodeCode), assignmentOptions },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.ProductionProcessCode), processOptions },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.EquipmentQuality), qualityOptions },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        excelService.AddSheetWithDropdown(workbook, dtoList, "Xe phuc vu va xe cau", new List<string> { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.HeaderId) }, dropdowns);
    }

    private async Task AddExcavatorSheet(
        XLWorkbook workbook, List<string> assignmentOptions, List<string> processOptions, List<string> qualityOptions)
    {
        var list = await _excavatorRepository.GetAllAsync(
            include: q => q
                .Include(e => e.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(e => e.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(e => e.Details),
            disableTracking: true);

        var dtoList = list.Select(header =>
        {
            var detail = header.Details.FirstOrDefault();
            return new ExcavatorAndBulldozerUnitPriceExcelDto
            {
                Id = header.Id,
                StartMonth = header.StartMonth.ToString("MM/yyyy"),
                EndMonth = header.EndMonth.ToString("MM/yyyy"),
                AssignmentCodeCode = header.AssignmentCode?.Code != null ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}" : string.Empty,
                EquipmentQuality = header.EquipmentQuality,
                ProductionProcessCode = header.ProductionProcess?.Code != null ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}" : string.Empty,
                FuelUnitPrice = detail?.FuelUnitPrice ?? 0,
                MaintenanceUnitPrice = detail?.MaintenanceUnitPrice ?? 0
            };
        });

        Dictionary<string, List<string>> dropdowns = new()
        {
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.AssignmentCodeCode), assignmentOptions },
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.ProductionProcessCode), processOptions },
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.EquipmentQuality), qualityOptions }
        };

        excelService.AddSheetWithDropdown(workbook, dtoList, "May xuc va may gat", new List<string> { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.Id) }, dropdowns);
    }
}