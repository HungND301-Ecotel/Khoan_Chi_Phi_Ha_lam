
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MechanizedTransportOverheadUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Queries;

public record ExportExcelMechanizedTransportOverheadUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelMechanizedTransportOverheadUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelMechanizedTransportOverheadUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<MechanizedTransportOverheadUnitPriceEntity> _repository = unitOfWork.GetRepository<MechanizedTransportOverheadUnitPriceEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<byte[]> Handle(ExportExcelMechanizedTransportOverheadUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(MechanizedTransportOverheadUnitPriceExcelDto.Id)];

        var list = await _repository.GetAllAsync(
            include: query => query.Include(m => m.ProcessGroup).ThenInclude(p => p!.Code),
            disableTracking: true);

        List<string> processGroupOptions = (await _processGroupRepository.GetAllAsync(
                include: query => query.Include(p => p.Code),
                selector: p => p.Code != null ? p.Code.Value + " - " + p.Name : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(MechanizedTransportOverheadUnitPriceExcelDto.ProcessGroupCode), processGroupOptions }
        };

        IEnumerable<MechanizedTransportOverheadUnitPriceExcelDto> dtoList = list.Select(m => new MechanizedTransportOverheadUnitPriceExcelDto
        {
            Id = m.Id,
            ProcessGroupCode = m.ProcessGroup?.Code != null
                ? $"{m.ProcessGroup.Code.Value} - {m.ProcessGroup.Name}"
                : string.Empty,
            StartMonth = m.StartMonth.ToString("MM/yyyy"),
            EndMonth = m.EndMonth.ToString("MM/yyyy"),
            LowValuePerishableSupplyUnitPrice = m.LowValuePerishableSupplyUnitPrice,
            ElectricityUnitPrice = m.ElectricityUnitPrice
        });

        return excelService.ExportToExcel(dtoList, "VT mau hong & Dien nang", hiddenProperties, dropdownConfigs);
    }
}
