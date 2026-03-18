using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallParameters;
using Application.Interfaces.Services;
using MediatR;
using LongwallParametersEntity = Domain.Entities.Index.LongwallParameters;


namespace Application.Catalog.Index.LongwallParameters.Queries;

public record ExportExcelLongwallParametersQuery() : IRequest<byte[]>;

public class ExportExcelLongwallParametersQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelLongwallParametersQuery, byte[]>
{
    private readonly IWriteRepository<LongwallParametersEntity> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParametersEntity>();
    public async Task<byte[]> Handle(ExportExcelLongwallParametersQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(LongwallParametersExcelDto.Id));

        var list = await _longwallParametersRepository.GetAllAsync(
            disableTracking: true);

        var dtoList = list.Select(l => new LongwallParametersExcelDto
        {
            Id = l.Id,
            Llc = l.Llc,
            Lkc = l.Lkc,
            Mk = l.Mk
        });

        return excelService.ExportToExcel(dtoList, "Thông số lò chợ", listHiddenProperty);
    }
}
