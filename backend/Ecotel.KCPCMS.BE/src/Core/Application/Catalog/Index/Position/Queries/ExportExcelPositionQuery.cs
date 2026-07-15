using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Position;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Index.Position.Queries;

public record ExportExcelPositionQuery() : IRequest<byte[]>;

public class ExportExcelPositionQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelPositionQuery, byte[]>
{
    private const string ActiveLabel = "Hoạt động";
    private const string InactiveLabel = "Không hoạt động";

    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<byte[]> Handle(ExportExcelPositionQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(PositionExcelDto.Id) };

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(PositionExcelDto.IsActiveName), new List<string> { ActiveLabel, InactiveLabel } }
        };

        var positions = await _positionRepository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var dtoList = positions
            .OrderBy(p => p.Level)
            .ThenBy(p => p.Name)
            .Select(p => new PositionExcelDto
            {
                Id = p.Id,
                Name = p.Name,
                Level = p.Level ?? 0,
                Description = p.Description,
                IsActiveName = p.IsActive ? ActiveLabel : InactiveLabel
            });

        return excelService.ExportToExcel(dtoList, "Chức vụ", hiddenProperties, dropdownConfigs);
    }
}