using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductionProcessEntity = Domain.Entities.Index.ProductionProcess;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Queries;

public record ExportExcelTransportUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelTransportUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelTransportUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _repository = unitOfWork.GetRepository<TransportUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcessEntity> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcessEntity>();
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<AssignmentCode> _equipmentRepository = unitOfWork.GetRepository<AssignmentCode>();

    public async Task<byte[]> Handle(ExportExcelTransportUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(TransportUnitPriceExcelDto.Id)];

        var list = await _repository.GetAllAsync(
            include: query => query
                .Include(t => t.ProductionProcess).ThenInclude(p => p!.Code)
                .Include(t => t.TransportRoute).ThenInclude(r => r!.Code)
                .Include(t => t.Department).ThenInclude(d => d!.Code)
                .Include(t => t.Equipment).ThenInclude(eq => eq!.Code),
            disableTracking: true);

        List<string> productionProcessOptions = (await _productionProcessRepository.GetAllAsync(
                predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTL,
                include: query => query.Include(p => p.Code),
                selector: p => p.Code != null ? p.Code.Value + " - " + p.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        List<string> transportRouteOptions = (await _transportRouteRepository.GetAllAsync(
                include: query => query.Include(r => r.Code),
                selector: r => r.Code != null ? r.Code.Value + " - " + r.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        List<string> departmentOptions = (await _departmentRepository.GetAllAsync(
                include: query => query.Include(d => d.Code),
                selector: d => d.Code != null ? d.Code.Value + " - " + d.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        List<string> equipmentOptions = (await _equipmentRepository.GetAllAsync(
                include: query => query.Include(eq => eq.Code),
                selector: eq => eq.Code != null ? eq.Code.Value + " - " + eq.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(TransportUnitPriceExcelDto.ProductionProcessCode), productionProcessOptions },
            { nameof(TransportUnitPriceExcelDto.TransportRouteCode), transportRouteOptions },
            { nameof(TransportUnitPriceExcelDto.DepartmentCode), departmentOptions },
            { nameof(TransportUnitPriceExcelDto.EquipmentCode), equipmentOptions },
            { nameof(TransportUnitPriceExcelDto.EquipmentQuality), new List<string> { "A", "B", "C" } },
            { nameof(TransportUnitPriceExcelDto.IsLowVolumeCase), new List<string> { "Có", "Không" } },
        };

        IEnumerable<TransportUnitPriceExcelDto> dtoList = list.Select(t => new TransportUnitPriceExcelDto
        {
            Id = t.Id,
            ProductionProcessCode = t.ProductionProcess?.Code != null
                ? $"{t.ProductionProcess.Code.Value} - {t.ProductionProcess.Name}"
                : string.Empty,
            TransportRouteCode = t.TransportRoute?.Code != null
                ? $"{t.TransportRoute.Code.Value} - {t.TransportRoute.Name}"
                : null,
            DepartmentCode = t.Department?.Code != null
                ? $"{t.Department.Code.Value} - {t.Department.Name}"
                : null,
            EquipmentCode = t.Equipment?.Code != null
                ? $"{t.Equipment.Code.Value} - {t.Equipment.Name}"
                : null,
            EquipmentQuality = t.EquipmentQuality,
            MaterialFuelUnitPrice = t.MaterialFuelUnitPrice,
            PowerUnitPrice = t.PowerUnitPrice,
            MaintenanceUnitPrice = t.MaintenanceUnitPrice,
            Quantity = t.Quantity,
            IsLowVolumeCase = t.IsLowVolumeCase,
            StartMonth = t.StartMonth.ToString("MM/yyyy"),
            EndMonth = t.EndMonth.ToString("MM/yyyy"),
        });

        return excelService.ExportToExcel(dtoList, "Don gia dinh muc VTL", hiddenProperties, dropdownConfigs);
    }
}