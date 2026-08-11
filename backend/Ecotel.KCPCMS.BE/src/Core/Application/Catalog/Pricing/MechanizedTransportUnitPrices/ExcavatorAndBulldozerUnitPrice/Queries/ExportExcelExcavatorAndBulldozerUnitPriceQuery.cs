using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Queries;


public record ExportExcelExcavatorAndBulldozerUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelExcavatorAndBulldozerUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelExcavatorAndBulldozerUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();

    public async Task<byte[]> Handle(ExportExcelExcavatorAndBulldozerUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(ExcavatorAndBulldozerUnitPriceExcelDto.Id)];

        var list = await _repository.GetAllAsync(
            include: query => query
                .Include(e => e.AssignmentCode).ThenInclude(a => a!.Code)
                .Include(e => e.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(e => e.Details),
            disableTracking: true);

        List<string> assignmentCodeOptions = (await _assignmentCodeRepository.GetAllAsync(
                include: query => query.Include(a => a.Code),
                selector: a => a.Code != null ? a.Code.Value + " - " + a.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        List<string> productionProcessOptions = (await _productionProcessRepository.GetAllAsync(
                predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTCG,
                include: query => query.Include(p => p.Code),
                selector: p => p.Code != null ? p.Code.Value + " - " + p.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.AssignmentCodeCode), assignmentCodeOptions },
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.ProductionProcessCode), productionProcessOptions },
            { nameof(ExcavatorAndBulldozerUnitPriceExcelDto.EquipmentQuality), new List<string> { "A", "B", "C" } }
        };

        IEnumerable<ExcavatorAndBulldozerUnitPriceExcelDto> dtoList = list.Select(e =>
        {
            var detail = e.Details.FirstOrDefault();
            return new ExcavatorAndBulldozerUnitPriceExcelDto
            {
                Id = e.Id,
                StartMonth = e.StartMonth.ToString("MM/yyyy"),
                EndMonth = e.EndMonth.ToString("MM/yyyy"),
                AssignmentCodeCode = e.AssignmentCode?.Code != null
                    ? $"{e.AssignmentCode.Code.Value} - {e.AssignmentCode.Name}"
                    : string.Empty,
                EquipmentQuality = e.EquipmentQuality,
                ProductionProcessCode = e.ProductionProcess?.Code != null
                    ? $"{e.ProductionProcess.Code.Value} - {e.ProductionProcess.Name}"
                    : string.Empty,
                FuelUnitPrice = detail?.FuelUnitPrice ?? 0,
                MaintenanceUnitPrice = detail?.MaintenanceUnitPrice ?? 0
            };
        });

        return excelService.ExportToExcel(dtoList, "May xuc va may gat", hiddenProperties, dropdownConfigs);
    }
}