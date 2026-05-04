using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Queries;

public record ExportExcelLowValuePerishableSupplyUnitPriceQuery(LowValuePerishableSupplyType Type) : IRequest<byte[]>;

public class ExportExcelLowValuePerishableSupplyUnitPriceQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelLowValuePerishableSupplyUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<byte[]> Handle(ExportExcelLowValuePerishableSupplyUnitPriceQuery request, CancellationToken cancellationToken)
    {
        List<string> hiddenProperties = [nameof(LowValuePerishableSupplyUnitPriceExcelDto.Id)];

        var list = await _repository.GetAllAsync(
            predicate: e => e.Type == request.Type,
            include: e => e.Include(x => x.Department).ThenInclude(d => d!.Code)
                .Include(x => x.ProcessGroup).ThenInclude(pg => pg!.Code),
            disableTracking: true);

        List<string> departmentCodes = (await _departmentRepository.GetAllAsync(
                include: d => d.Include(x => x.Code),
                selector: d => d.Code != null ? d.Code.Value : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        ProcessGroupType processGroupType = request.Type == LowValuePerishableSupplyType.TunnelExcavation
            ? ProcessGroupType.DL
            : ProcessGroupType.LC;

        List<string> processGroupCodes = (await _processGroupRepository.GetAllAsync(
            predicate: pg => (pg.FixedKey != null ? pg.FixedKey.Type : pg.Type) == processGroupType,
            include: pg => pg.Include(x => x.Code).Include(x => x.FixedKey),
            selector: pg => pg.FixedKey != null ? pg.FixedKey.Key : pg.Code != null ? pg.Code.Value : string.Empty,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        Dictionary<string, List<string>> dropdownConfigs = new()
        {
            { nameof(LowValuePerishableSupplyUnitPriceExcelDto.DepartmentCode), departmentCodes },
            { nameof(LowValuePerishableSupplyUnitPriceExcelDto.ProcessGroupCode), processGroupCodes },
        };

        IEnumerable<LowValuePerishableSupplyUnitPriceExcelDto> dtoList = list.Select(e => new LowValuePerishableSupplyUnitPriceExcelDto
        {
            Id = e.Id,
            DepartmentCode = e.Department?.Code?.Value ?? string.Empty,
            ProcessGroupCode = e.ProcessGroup?.FixedKey?.Key ?? string.Empty,
            StartMonth = e.StartMonth.ToString("MM/yyyy"),
            EndMonth = e.EndMonth.ToString("MM/yyyy"),
            TotalPrice = e.TotalPrice,
        });

        string sheetName = request.Type == LowValuePerishableSupplyType.TunnelExcavation
            ? "VT mau hong dao lo"
            : "VT mau hong lo cho";

        return excelService.ExportToExcel(dtoList, sheetName, hiddenProperties, dropdownConfigs);
    }
}