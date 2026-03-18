using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.ProcessGroups.Queries;

public record ExportExcelProcessGroupQuery() : IRequest<byte[]>;

public class ExportExcelProcessGroupQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelProcessGroupQuery, byte[]>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<byte[]> Handle(ExportExcelProcessGroupQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(ProcessGroupExcelDto.Id));

        var list = await _processGroupRepository.GetAllAsync(
            include: p => p.Include(p => p.Code),
            disableTracking: true);

        var dtoList = list.Select(l => new ProcessGroupExcelDto
        {
            Id = l.Id,
            Name = l.Name,
            Code = l.Code?.Value ?? ""
        });

        return excelService.ExportToExcel(dtoList, "Nhóm công đoạn sản xuất", listHiddenProperty);
    }
}