using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelHardnessQuery() : IRequest<byte[]>;

public class ExportExcelHardnessQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelHardnessQuery, byte[]>
{
    private readonly IWriteRepository<Hardness> hardnessRepository = unitOfWork.GetRepository<Hardness>();
    public async Task<byte[]> Handle(ExportExcelHardnessQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(HardnessExcelDto.Id));

        var list = await hardnessRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<HardnessExcelDto>>(), "Độ kiên cố than đá", listHiddenProperty);
    }
}