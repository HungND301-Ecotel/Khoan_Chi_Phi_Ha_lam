using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelTechnologyQuery() : IRequest<byte[]>;

public class ExportExcelTechnologyQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelTechnologyQuery, byte[]>
{
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    public async Task<byte[]> Handle(ExportExcelTechnologyQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(TechnologyExcelDto.Id));

        var list = await _technologyRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<TechnologyExcelDto>>(), "Công nghệ", listHiddenProperty);
    }
}
