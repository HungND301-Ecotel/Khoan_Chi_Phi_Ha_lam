using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Department;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Department.Queries;

public record ExportExcelDepartmentQuery() : IRequest<byte[]>;

public class ExportExcelDepartmentQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelDepartmentQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Department>();

    public async Task<byte[]> Handle(ExportExcelDepartmentQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(DepartmentExcelDto.Id));

        var list = await _departmentRepository.GetAllAsync(
            include: q => q.Include(d => d.Code),
            disableTracking: true);

        var dtoList = list.Select(l => new DepartmentExcelDto
        {
            Id = l.Id,
            Code = l.Code?.Value ?? string.Empty,
            Name = l.Name
        });

        return excelService.ExportToExcel(dtoList, "Đơn vị", listHiddenProperty);
    }
}
