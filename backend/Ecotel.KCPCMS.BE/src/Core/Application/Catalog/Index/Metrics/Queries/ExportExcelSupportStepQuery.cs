using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelSupportStepQuery() : IRequest<byte[]>;

public class ExportExcelSupportStepQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelSupportStepQuery, byte[]>
{
    private readonly IWriteRepository<SupportStep> hardnessRepository = unitOfWork.GetRepository<SupportStep>();
    public async Task<byte[]> Handle(ExportExcelSupportStepQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(SupportStepExcelDto.Id));

        var list = await hardnessRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<SupportStepExcelDto>>(), "Bước chống", listHiddenProperty);
    }
}