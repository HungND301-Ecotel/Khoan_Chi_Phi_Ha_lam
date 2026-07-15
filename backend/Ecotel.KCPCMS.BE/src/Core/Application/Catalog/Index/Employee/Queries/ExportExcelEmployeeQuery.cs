using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Employee.Queries;

public record ExportExcelEmployeeQuery() : IRequest<byte[]>;

public class ExportExcelEmployeeQueryHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelEmployeeQuery, byte[]>
{
    private const string MaleLabel = "Nam";
    private const string FemaleLabel = "Nữ";

    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Position>();
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Department>();

    public async Task<byte[]> Handle(ExportExcelEmployeeQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(EmployeeExcelDto.Id) };

        var positions = await _positionRepository.GetAllAsync(predicate: p => p.IsActive, disableTracking: true);
        var departments = await _departmentRepository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            {
                nameof(EmployeeExcelDto.PositionName),
                positions.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList()
            },
            {
                nameof(EmployeeExcelDto.DepartmentName),
                departments.Select(d => d.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList()
            },
            {
                nameof(EmployeeExcelDto.GenderName),
                new List<string> { MaleLabel, FemaleLabel }
            }
        };

        var employees = await _employeeRepository.GetAllAsync(
            include: q => q
                .Include(e => e.Position)
                .Include(e => e.Department)
                .Include(e => e.User),
            disableTracking: true);

        var dtoList = employees
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeExcelDto
            {
                Id = e.Id,
                FullName = e.FullName,
                PositionName = e.Position != null ? e.Position.Name : string.Empty,
                DepartmentName = e.Department != null ? e.Department.Name : string.Empty,
                UserName = e.User != null ? e.User.UserName : string.Empty,
                Email = e.User != null ? e.User.Email : string.Empty,
                PhoneNumber = e.User != null ? e.User.PhoneNumber : string.Empty,
                Cccd = e.Cccd,
                Province = e.Province,
                District = e.District,
                Ward = e.Ward,
                StreetAddress = e.StreetAddress,
                Dob = e.Dob,
                GenderName = e.Gender.HasValue ? (e.Gender.Value ? MaleLabel : FemaleLabel) : string.Empty
            });

        return excelService.ExportToExcel(dtoList, "Nhân viên", hiddenProperties, dropdownConfigs);
    }
}