using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelHaulDistanceQuery() : IRequest<byte[]>;

public class ExportExcelHaulDistanceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelHaulDistanceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.HaulDistance> _repository =
        unitOfWork.GetRepository<Domain.Entities.Index.HaulDistance>();

    public async Task<byte[]> Handle(ExportExcelHaulDistanceQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(HaulDistanceExcelDto.Id) };

        var haulDistances = await _repository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var dtoList = haulDistances
            .OrderBy(c => c.Value)
            .Select(c => new HaulDistanceExcelDto
            {
                Id = c.Id,
                Value = c.Value
            });

        return excelService.ExportToExcel(dtoList, "Cung độ vận tải ", hiddenProperties);
    }
}