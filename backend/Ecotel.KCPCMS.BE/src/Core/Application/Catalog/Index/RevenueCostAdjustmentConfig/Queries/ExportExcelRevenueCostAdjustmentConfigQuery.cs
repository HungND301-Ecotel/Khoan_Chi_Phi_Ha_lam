using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Application.Interfaces.Services;
using MediatR;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Queries;

public record ExportExcelRevenueCostAdjustmentConfigQuery() : IRequest<byte[]>;

public class ExportExcelRevenueCostAdjustmentConfigQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelRevenueCostAdjustmentConfigQuery, byte[]>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<byte[]> Handle(ExportExcelRevenueCostAdjustmentConfigQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string>
        {
            nameof(RevenueCostAdjustmentConfigExcelDto.Id)
        };

        var list = await _revenueCostAdjustmentConfigRepository.GetAllAsync(disableTracking: true);

        var dtoList = list.Select(x => new RevenueCostAdjustmentConfigExcelDto
        {
            Id = x.Id,
            ProfitConditionDisplay = x.ProfitConditionDisplay,
            RateDisplay = x.RateDisplay,
            Description = x.Description
        });

        return excelService.ExportToExcel(dtoList, "Ty le dieu chinh thu chi", hiddenProperties);
    }
}
