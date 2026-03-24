using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.StoneClampRatio.Queries;

public record ExportExcelStoneClampRatioQuery() : IRequest<byte[]>;

public class ExportExcelStoneClampRatioQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelStoneClampRatioQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _processRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository = unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    public async Task<byte[]> Handle(ExportExcelStoneClampRatioQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(StoneClampRatioExcelDto.Id));

        var list = await _stoneClampRatioRepository.GetAllAsync(
            disableTracking: true);

        var process = await _processRepository.GetAllAsync(
            selector: u => u.Code.Value,
            include: u => u.Include(u => u.Code),
            disableTracking: true);

        var hardness = await _hardnessRepository.GetAllAsync(
            selector: u => u.Value,
            disableTracking: true);

        var dtoList = list.Select(s => new StoneClampRatioExcelDto
        {
            Id = s.Id,
            Value = s.Value,
        });

        return excelService.ExportToExcel(dtoList, "Đơn vị tính", listHiddenProperty);
    }
}