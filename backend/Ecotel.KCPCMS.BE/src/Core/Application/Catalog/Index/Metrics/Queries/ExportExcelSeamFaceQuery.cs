using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelSeamFaceQuery() : IRequest<byte[]>;

public class ExportExcelSeamFaceQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelSeamFaceQuery, byte[]>
{
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    public async Task<byte[]> Handle(ExportExcelSeamFaceQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(SeamFaceExcelDto.Id));

        var list = await _seamFaceRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<SeamFaceExcelDto>>(), "Mặt nứt", listHiddenProperty);
    }
}
