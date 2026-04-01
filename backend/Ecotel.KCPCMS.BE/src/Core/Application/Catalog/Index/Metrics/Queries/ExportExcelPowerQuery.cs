using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelPowerQuery() : IRequest<byte[]>;

public class ExportExcelPowerQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelPowerQuery, byte[]>
{
    private readonly IWriteRepository<Power> PowerRepository = unitOfWork.GetRepository<Power>();
    public async Task<byte[]> Handle(ExportExcelPowerQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(PowerExcelDto.Id));

        var list = await PowerRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<PowerExcelDto>>(), "Công suất", listHiddenProperty);
    }
}