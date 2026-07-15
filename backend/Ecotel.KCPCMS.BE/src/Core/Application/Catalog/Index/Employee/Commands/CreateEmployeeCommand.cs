using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Verification;
using Application.Dto.Catalog.Employee;
using Application.Interfaces.Services;
using Application.Utility;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Shared.Constants;
using Shared.Constants.EmailTemplate;

namespace Application.Catalog.Index.Employee.Commands;

public record CreateEmployeeCommand(CreateEmployeeDto CreateModel) : IRequest<bool>;

public class CreateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    IVerificationService verificationService)
    : IRequestHandler<CreateEmployeeCommand, bool>
{
    private const string DefaultPassword = "123456";

    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<User> _userRepository =
        unitOfWork.GetRepository<User>();
    private readonly IWriteRepository<Role> _roleRepository =
        unitOfWork.GetRepository<Role>();

    public async Task<bool> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.AnyAsync(u => u.NormalizedUserName == request.CreateModel.UserName.Trim().ToUpperInvariant()))
        {
            throw new ConflictException("Tên đăng nhập đã tồn tại.");
        }

        if (await _userRepository.AnyAsync(u => u.NormalizedEmail == request.CreateModel.Email.Trim().ToUpperInvariant()))
        {
            throw new ConflictException("Email đã tồn tại.");
        }
        if (await _userRepository.AnyAsync(u => u.PhoneNumber == request.CreateModel.PhoneNumber.Trim()))
        {
            throw new ConflictException("Số điện thoại đã tồn tại.");
        }
        var userRole = await _roleRepository.GetFirstOrDefaultAsync(
            predicate: r => r.RoleType == RoleType.User,
            disableTracking: true)
            ?? throw new NotFoundException("Không tìm thấy Role User mặc định.");

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var user = new User(request.CreateModel.UserName.Trim(), request.CreateModel.Email.Trim(),request.CreateModel.PhoneNumber.Trim());
            user.SetPassword(Utils.ComputeHash(DefaultPassword));
            await _userRepository.InsertAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            user.AddRole(userRole.Id, RoleType.User);
            _userRepository.Update(user);

            var employee = Domain.Entities.Index.Employee.Create(
                fullName: request.CreateModel.FullName.Trim(),
                userId: user.Id,
                positionId: request.CreateModel.PositionId,
                departmentId: request.CreateModel.DepartmentId,
                avatarUrl: string.Empty,
                dob: null,
                gender: null,
                cccd: request.CreateModel.Cccd,
                province: request.CreateModel.Province,
                district: null,
                ward: string.Empty,
                streetAddress: string.Empty);

            await _employeeRepository.InsertAsync(employee, cancellationToken);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            await verificationService.SaveAndSendVerificationByEmail(new SendVerificationEmailModel
            {
                Email = request.CreateModel.Email.Trim(),
                Locale = EmailSupportLanguageConst.Vietnamese,
                Mode = UserVerificationMode.VerifyCurrentUserEmailByLinkOnly
            }, user.Id);

            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}