using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Interfaces.Services;
using MediatR;
using CuttingThicknessEntity = Domain.Entities.Index.CuttingThickness;

namespace Application.Catalog.Index.CuttingThickness.Queries;

public record ExportExcelCuttingThicknessQuery() : IRequest<byte[]>;

public class ExportExcelCuttingThicknessQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelCuttingThicknessQuery, byte[]>
{
    private readonly IWriteRepository<CuttingThicknessEntity> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThicknessEntity>();

    public async Task<byte[]> Handle(ExportExcelCuttingThicknessQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(CuttingThicknessExcelDto.Id));

        var list = await _cuttingThicknessRepository.GetAllAsync(
            disableTracking: true);

        var dtoList = list.Select(l => new CuttingThicknessExcelDto
        {
            Id = l.Id,
            Value = l.Value
        });

        return excelService.ExportToExcel(dtoList, "Chi?u dày c?t", listHiddenProperty);
    }
}
