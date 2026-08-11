using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportLocation;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.TransportLocation.Queries;

public record ExportExcelTransportLocationQuery() : IRequest<byte[]>;

public class ExportExcelTransportLocationQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelTransportLocationQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository =
        unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<byte[]> Handle(ExportExcelTransportLocationQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(TransportLocationExcelDto.Id) };

        var locations = await _repository.GetAllAsync(
            predicate: _ => true,
            include: x => x.Include(t => t.Code),
            disableTracking: true);

        var dtoList = locations
            .OrderBy(t => t.Name)
            .Select(t => new TransportLocationExcelDto
            {
                Id = t.Id,
                Code = t.Code?.Value ?? string.Empty,
                Name = t.Name,
                Note = t.Note,
                LocationType = t.LocationType
            });

        return excelService.ExportToExcel(dtoList, "Vị trí nhận, đổ", hiddenProperties);
    }
}