using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using Application.Interfaces.Services;
using MediatR;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Queries;

public record ExportExcelSavingsRateConfigQuery() : IRequest<byte[]>;

public class ExportExcelSavingsRateConfigQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelSavingsRateConfigQuery, byte[]>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<byte[]> Handle(ExportExcelSavingsRateConfigQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string>
        {
            nameof(SavingsRateConfigExcelDto.Id)
        };

        var list = await _savingsRateConfigRepository.GetAllAsync(disableTracking: true);

        var dtoList = list.Select(x => new SavingsRateConfigExcelDto
        {
            Id = x.Id,
            RevenueDisplay = x.RevenueDisplay,
            SavingsRateDisplay = x.SavingsRateDisplay,
            Description = x.Description
        });

        return excelService.ExportToExcel(dtoList, "Ty le tiet kiem", hiddenProperties);
    }
}
