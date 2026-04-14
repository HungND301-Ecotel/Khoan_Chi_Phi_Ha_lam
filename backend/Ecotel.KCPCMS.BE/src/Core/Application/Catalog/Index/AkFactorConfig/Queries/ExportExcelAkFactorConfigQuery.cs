using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;

namespace Application.Catalog.Index.AkFactorConfig.Queries;

public record ExportExcelAkFactorConfigQuery() : IRequest<byte[]>;

public class ExportExcelAkFactorConfigQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelAkFactorConfigQuery, byte[]>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();

    public async Task<byte[]> Handle(ExportExcelAkFactorConfigQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string>
        {
            nameof(AkFactorConfigExcelDto.Id)
        };

        var list = await _akFactorConfigRepository.GetAll()
            .Include(x => x.ProcessGroup)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtoList = list.Select(x => new AkFactorConfigExcelDto
        {
            Id = x.Id,
            ProcessGroupCode = x.ProcessGroup?.Code?.Value ?? string.Empty,
            AkDiffDisplay = x.AkDiffDisplay,
            AdjustmentRateDisplay = x.AdjustmentRateDisplay,
            Description = x.Description
        });

        return excelService.ExportToExcel(dtoList, "He so Ak", hiddenProperties);
    }
}
