using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Queries;

public record ExportExcelServiceAndCraneVehicleUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelServiceAndCraneVehicleUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelServiceAndCraneVehicleUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _repository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<HaulDistance> _haulDistanceRepository = unitOfWork.GetRepository<HaulDistance>();

    public async Task<byte[]> Handle(ExportExcelServiceAndCraneVehicleUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(ServiceAndCraneVehicleUnitPriceExcelDto.HeaderId)];

        var list = await _repository.GetAllAsync(
            include: q => q
                .Include(s => s.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(s => s.ProductionProcess).ThenInclude(p => p!.Code)
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

        var haulDistanceOptions = (await _haulDistanceRepository.GetAllAsync(
                selector: h => h.Value,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.AssignmentCodeCode), assignmentCodeOptions },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.ProductionProcessCode), productionProcessOptions },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.EquipmentQuality), new List<string> { "A", "B", "C" } },
            { nameof(ServiceAndCraneVehicleUnitPriceExcelDto.HaulDistanceValue), haulDistanceOptions }
        };

        var dtoList = new List<ServiceAndCraneVehicleUnitPriceExcelDto>();
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
                dtoList.Add(new ServiceAndCraneVehicleUnitPriceExcelDto
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

        return excelService.ExportToExcel(dtoList, "Xe phuc vu va xe cau", hiddenProperties, dropdownConfigs);
    }
}