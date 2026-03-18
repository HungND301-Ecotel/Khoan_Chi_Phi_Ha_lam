using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Passport;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using MediatR;
using PassportEntity = Domain.Entities.Index.Passport;


namespace Application.Catalog.Index.Passport.Queries;

public record ExportExcelPassportQuery() : IRequest<byte[]>;

public class ExportExcelPassportQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelPassportQuery, byte[]>
{
    private readonly IWriteRepository<PassportEntity> _passportRepository = unitOfWork.GetRepository<PassportEntity>();
    public async Task<byte[]> Handle(ExportExcelPassportQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(ProcessGroupExcelDto.Id));

        var list = await _passportRepository.GetAllAsync(
            disableTracking: true);

        var dtoList = list.Select(l => new PassportExcelDto
        {
            Id = l.Id,
            Name = l.Name,
            Sd = l.Sd,
            Sc = l.Sc
        });

        return excelService.ExportToExcel(dtoList, "Hộ chiếu, Sđ, Sc", listHiddenProperty);
    }
}