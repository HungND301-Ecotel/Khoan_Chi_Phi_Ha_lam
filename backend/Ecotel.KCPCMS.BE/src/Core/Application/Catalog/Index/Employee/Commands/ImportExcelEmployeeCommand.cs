using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Application.Catalog.Index.Employee.Commands;

public record ImportExcelEmployeeCommand(IFormFile File) : IRequest<bool>;

public class ImportExcelEmployeeCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher) : IRequestHandler<ImportExcelEmployeeCommand, bool>
{
    private const string DefaultPassword = "123456";
    private const string MaleLabel = "Nam";

    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Position>();
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Department>();
    private readonly IWriteRepository<User> _userRepository = unitOfWork.GetRepository<User>();
    private readonly IWriteRepository<Role> _roleRepository = unitOfWork.GetRepository<Role>();

    public async Task<bool> Handle(ImportExcelEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<EmployeeExcelDto>(stream).ToList();

        var dbEmployees = await _employeeRepository.GetAllAsync(predicate: _ => true, disableTracking: true);
        var allPositions = await _positionRepository.GetAllAsync(predicate: _ => true, disableTracking: true);
        var allDepartments = await _departmentRepository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var userRole = await _roleRepository.GetFirstOrDefaultAsync(
            predicate: r => r.RoleType == RoleType.User,
            disableTracking: true) ?? throw new NotFoundException("Không tìm thấy Role User mặc định.");

        var updatePayloads = new List<(Domain.Entities.Index.Employee Entity, EmployeeExcelDto Dto, int PositionId, Guid DepartmentId, bool? Gender)>();
        var addPayloads = new List<(EmployeeExcelDto Dto, int PositionId, Guid DepartmentId, bool? Gender)>();

        foreach (var dto in dtos)
        {
            var rowNumber = dtos.IndexOf(dto) + 2;

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new BadRequestException($"Họ và tên không được để trống ở dòng {rowNumber}.");
            }

            var position = allPositions.FirstOrDefault(p =>
                p.Name.Trim().Equals(dto.PositionName?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new BadRequestException($"Không tìm thấy chức vụ '{dto.PositionName}' ở dòng {rowNumber}.");

            var department = allDepartments.FirstOrDefault(d =>
                d.Name.Trim().Equals(dto.DepartmentName?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new BadRequestException($"Không tìm thấy phòng ban '{dto.DepartmentName}' ở dòng {rowNumber}.");

            bool? gender = string.IsNullOrWhiteSpace(dto.GenderName)
                ? null
                : string.Equals(dto.GenderName.Trim(), MaleLabel, StringComparison.OrdinalIgnoreCase);

            if (dto.Id.HasValue)
            {
                var entityToUpdate = dbEmployees.FirstOrDefault(x => x.Id == dto.Id.Value)
                    ?? throw new NotFoundException($"Không tìm thấy nhân viên Id={dto.Id.Value} ở dòng {rowNumber}.");

                updatePayloads.Add((entityToUpdate, dto, position.Id, department.Id, gender));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.UserName) || dto.UserName.Trim().Length < 3)
                {
                    throw new BadRequestException($"Tên đăng nhập không hợp lệ ở dòng {rowNumber}.");
                }

                if (await _userRepository.AnyAsync(u => u.NormalizedUserName == dto.UserName.Trim().ToUpperInvariant()))
                {
                    throw new ConflictException($"Tên đăng nhập '{dto.UserName}' đã tồn tại ở dòng {rowNumber}.");
                }

                addPayloads.Add((dto, position.Id, department.Id, gender));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            foreach (var (entity, dto, positionId, departmentId, gender) in updatePayloads)
            {
                entity.UpdateEmployee(
                    fullName: dto.FullName.Trim(),
                    positionId: positionId,
                    departmentId: departmentId,
                    avatarUrl: entity.Avatar,
                    dob: dto.Dob,
                    gender: gender,
                    cccd: dto.Cccd?.Trim() ?? string.Empty,
                    province: dto.Province?.Trim() ?? string.Empty,
                    district: dto.District?.Trim(),
                    ward: dto.Ward?.Trim() ?? string.Empty,
                    streetAddress: dto.StreetAddress?.Trim() ?? string.Empty);

                _employeeRepository.Update(entity);
            }

            foreach (var (dto, positionId, departmentId, gender) in addPayloads)
            {
                var userName = dto.UserName.Trim();
                var user = new User(userName, string.IsNullOrWhiteSpace(dto.Email) ? $"{userName}@company.com" : dto.Email.Trim(), dto.PhoneNumber.Trim());
                user.SetPassword(passwordHasher.HashPassword(user, DefaultPassword));

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.SetPhoneNumber(dto.PhoneNumber.Trim());
                }

                await _userRepository.InsertAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync();

                user.AddRole(userRole.Id, RoleType.User);
                _userRepository.Update(user);

                var employee = Domain.Entities.Index.Employee.Create(
                    fullName: dto.FullName.Trim(),
                    positionId: positionId,
                    departmentId: departmentId,
                    avatarUrl: string.Empty,
                    dob: dto.Dob,
                    gender: gender,
                    cccd: dto.Cccd?.Trim() ?? string.Empty,
                    province: dto.Province?.Trim() ?? string.Empty,
                    district: dto.District?.Trim(),
                    ward: dto.Ward?.Trim() ?? string.Empty,
                    streetAddress: dto.StreetAddress?.Trim() ?? string.Empty,
                    userId: user.Id);

                await _employeeRepository.InsertAsync(employee, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}