using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.UnitOfMeasure;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.UnitOfMeasures.Queries;

public record ExportExcelUnitOfMeasureQuery() : IRequest<byte[]>;

public class ExportExcelUnitOfMeasureQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelUnitOfMeasureQuery, byte[]>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<byte[]> Handle(ExportExcelUnitOfMeasureQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(UnitOfMeasureExcelDto.Id));

        var list = await _unitOfMeasureRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<UnitOfMeasureExcelDto>>(), "Đơn vị tính", listHiddenProperty);
    }
}