using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AssignmentCodes.Queries;

public record ExportExcelAssignmentCodeQuery() : IRequest<byte[]>;

public class ExportExcelAssignmentCodeQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelAssignmentCodeQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();
    public async Task<byte[]> Handle(ExportExcelAssignmentCodeQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(AssignmentCodeExcelDto.Id));

        var list = await _assignmentCodeRepository.GetAllAsync(
            include: s => s.Include(s => s.UnitOfMeasure).Include(s => s.Code),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(AssignmentCodeExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() }
        };

        var dtoList = list.Select(s => new AssignmentCodeExcelDto
        {
            Id = s.Id,
            Code = s.Code?.Value ?? "",
            Name = s.Name,
            UnitOfMeasureName = s.UnitOfMeasure?.Name ?? ""
        });

        return excelService.ExportToExcel(
            data: dtoList,
            sheetName: "Mã giao khoán",
            hiddenProperties: listHiddenProperty,
            dropdownData: dropdownConfigs);
    }
}