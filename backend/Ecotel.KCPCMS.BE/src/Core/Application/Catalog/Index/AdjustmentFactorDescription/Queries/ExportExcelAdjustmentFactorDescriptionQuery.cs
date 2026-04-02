using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Queries;

public record ExportExcelAdjustmentFactorDescriptionQuery() : IRequest<byte[]>;

public class ExportExcelAdjustmentFactorDescriptionQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelAdjustmentFactorDescriptionQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    public async Task<byte[]> Handle(ExportExcelAdjustmentFactorDescriptionQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(AdjustmentFactorDescriptionExcelDto.Id));

        var list = await _adjustmentFactorDescriptionRepository.GetAllAsync(
            include: s => s.Include(s => s.AdjustmentFactor).ThenInclude(a => a.Code!)
                            .Include(s => s.AdjustmentFactor).ThenInclude(a => a.ProcessGroup).ThenInclude(g => g.Code!),
            disableTracking: true);

        var adjustmentFactors = await _adjustmentFactorRepository.GetAllAsync(
            selector: u => $"{u.ProcessGroup.Code.Value} - {u.Code.Value}",
            include: u => u
                .Include(u => u.Code)
                .Include(u => u.ProcessGroup).ThenInclude(p => p.Code),
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(AdjustmentFactorDescriptionExcelDto.AdjustmentFactorCode), adjustmentFactors.ToList() }
        };

        var dtoList = list.Select(s => new AdjustmentFactorDescriptionExcelDto
        {
            Id = s.Id,
            AdjustmentFactorCode = (s.AdjustmentFactor?.ProcessGroup!.Code!.Value + " - " + s.AdjustmentFactor!.Code?.Value) ?? "",
            Description = s.Description,
            MaintenanceAdjustmentValue = s.MaintenanceAdjustmentValue,
            ElectricityAdjustmentValue = s.ElectricityAdjustmentValue
        });

        return excelService.ExportToExcel(dtoList.OrderBy(d => d.AdjustmentFactorCode).ThenBy(d => d.Description), "Diễn giải hệ số điều chỉnh", listHiddenProperty, dropdownConfigs);
    }
}