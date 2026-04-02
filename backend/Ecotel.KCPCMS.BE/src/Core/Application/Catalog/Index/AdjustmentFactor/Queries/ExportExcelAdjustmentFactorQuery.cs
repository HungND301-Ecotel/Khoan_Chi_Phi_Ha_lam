using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record ExportExcelAdjustmentFactorQuery() : IRequest<byte[]>;

public class ExportExcelAdjustmentFactorQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelAdjustmentFactorQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    public async Task<byte[]> Handle(ExportExcelAdjustmentFactorQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(AdjustmentFactorExcelDto.Id));
        listHiddenProperty.Add(nameof(AdjustmentFactorExcelDto.Type));

        var list = await _adjustmentFactorRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProcessGroup).ThenInclude(s => s.Code)
                .Include(s => s.Code!),
            disableTracking: true);

        var processGroups = await _processGroupRepository.GetAllAsync(
            selector: u => u.Code.Value,
            include: u => u.Include(u => u.Code),
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(AdjustmentFactorExcelDto.ProcessGroupCode), processGroups.ToList() }
        };

        var dtoList = list.Select(s => new AdjustmentFactorExcelDto
        {
            Id = s.Id,
            Type = (int)s.Type,
            Code = s.Code?.Value ?? "",
            Name = s.Name,
            ProcessGroupCode = s.ProcessGroup?.Code?.Value ?? ""
        });

        return excelService.ExportToExcel(dtoList.OrderBy(d => d.ProcessGroupCode).ThenBy(d => d.Code), "Hệ số điều chỉnh", listHiddenProperty, dropdownConfigs);
    }
}