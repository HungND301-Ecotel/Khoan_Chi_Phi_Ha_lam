using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Queries;

public record ExportExcelWasteSuctionTruckUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelWasteSuctionTruckUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelWasteSuctionTruckUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _repository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<HaulDistance> _haulDistanceRepository = unitOfWork.GetRepository<HaulDistance>();

    public async Task<byte[]> Handle(ExportExcelWasteSuctionTruckUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(WasteSuctionTruckUnitPriceExcelDto.HeaderId)];

        var list = await _repository.GetAllAsync(
            include: q => q
                .Include(w => w.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(w => w.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(w => w.Details).ThenInclude(d => d.HaulDistance),
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

        var haulDistanceOptions = (await _haulDistanceRepository.GetAllAsync(
                selector: h => h.Value,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(WasteSuctionTruckUnitPriceExcelDto.AssignmentCodeCode), assignmentCodeOptions },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.ProductionProcessCode), productionProcessOptions },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.EquipmentQuality), new List<string> { "A", "B", "C" } },
            { nameof(WasteSuctionTruckUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        var dtoList = new List<WasteSuctionTruckUnitPriceExcelDto>();
        foreach (var header in list)
        {
            var assignmentCodeText = header.AssignmentCode?.Code != null
                ? $"{header.AssignmentCode.Code.Value} - {header.AssignmentCode.Name}"
                : string.Empty;
            var productionProcessText = header.ProductionProcess?.Code != null
                ? $"{header.ProductionProcess.Code.Value} - {header.ProductionProcess.Name}"
                : string.Empty;

            foreach (var detail in header.Details)
            {
                dtoList.Add(new WasteSuctionTruckUnitPriceExcelDto
                {
                    HeaderId = header.Id,
                    StartMonth = header.StartMonth.ToString("MM/yyyy"),
                    EndMonth = header.EndMonth.ToString("MM/yyyy"),
                    AssignmentCodeCode = assignmentCodeText,
                    EquipmentQuality = header.EquipmentQuality,
                    ProductionProcessCode = productionProcessText,
                    HaulDistanceValue = detail.HaulDistance?.Value,
                    FuelUnitPrice = detail.FuelUnitPrice,
                    MaintenanceUnitPrice = detail.MaintenanceUnitPrice
                });
            }
        }

        return excelService.ExportToExcel(dtoList, "Xe hut bun chat thai", hiddenProperties, dropdownConfigs);
    }
}