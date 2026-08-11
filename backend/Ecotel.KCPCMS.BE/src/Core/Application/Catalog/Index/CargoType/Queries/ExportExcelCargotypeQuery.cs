using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CargoType;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.CargoType.Queries;

public record ExportExcelCargoTypeQuery() : IRequest<byte[]>;

public class ExportExcelCargoTypeQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelCargoTypeQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _cargoTypeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<byte[]> Handle(ExportExcelCargoTypeQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(CargoTypeExcelDto.Id) };

        var cargoTypes = await _cargoTypeRepository.GetAllAsync(
            predicate: _ => true,
            include: x => x.Include(c => c.Code),
            disableTracking: true);

        var dtoList = cargoTypes
            .OrderBy(c => c.Name)
            .Select(c => new CargoTypeExcelDto
            {
                Id = c.Id,
                Code = c.Code?.Value ?? string.Empty,
                Name = c.Name,
                Note = c.Note
            });

        return excelService.ExportToExcel(dtoList, "Chủng loại hàng", hiddenProperties);
    }
}