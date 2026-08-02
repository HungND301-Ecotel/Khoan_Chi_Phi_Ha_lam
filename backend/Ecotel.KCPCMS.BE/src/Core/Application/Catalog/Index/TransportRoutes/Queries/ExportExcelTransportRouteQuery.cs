using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportRoute;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.TransportRoutes.Queries;

public record ExportExcelTransportRouteQuery() : IRequest<byte[]>;

public class ExportExcelTransportRouteQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelTransportRouteQuery, byte[]>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();

    public async Task<byte[]> Handle(ExportExcelTransportRouteQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(TransportRouteExcelDto.Id));

        var list = await _transportRouteRepository.GetAllAsync(
            include: query => query
                .Include(t => t.Code)
                .Include(t => t.ProductionProcess)
                    .ThenInclude(p => p.Code),
            disableTracking: true);

        var excelDtos = list.Select(t => new TransportRouteExcelDto
        {
            Id = t.Id,
            Code = t.Code?.Value ?? string.Empty,
            Name = t.Name,
            Note = t.Note,
            ProductionProcessCode = t.ProductionProcess?.Code?.Value ?? string.Empty,
            IsSpecialLowVolume = t.IsSpecialLowVolume
        }).ToList();

        var vtlProductionProcessCodes = await _productionProcessRepository.GetAllAsync(
            predicate: p => p.ProcessGroup != null && p.ProcessGroup.FixedKey != null && p.ProcessGroup.FixedKey.Type == FixedKeyType.VTL,
            include: query => query.Include(p => p.Code),
            disableTracking: true);

        var dropdownData = new Dictionary<string, List<string>>
        {
            { nameof(TransportRouteExcelDto.ProductionProcessCode), vtlProductionProcessCodes.Select(p => p.Code!.Value).ToList() },
            { nameof(TransportRouteExcelDto.IsSpecialLowVolume), new List<string> { "True", "False" } }
        };

        return excelService.ExportToExcel(excelDtos, "Tuyến vận tải", listHiddenProperty, dropdownData);
    }
}