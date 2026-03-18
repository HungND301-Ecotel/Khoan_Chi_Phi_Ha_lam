using Application.Catalog.Users.Commands;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Accounts;
using Application.Dto.Authorization.Role;
using Application.Dto.Authorization.Verification;
using Application.Dto.Persistence.Catalog.User;
using Application.Interfaces.Infrastructures.Integrates.External.Service.Email;
using Application.Interfaces.Services;
using Application.Utility;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using Humanizer;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Constants.EmailTemplate;
using User = Domain.Entities.Identity.User;

namespace Infrastructure.Services.Identity;

public class UserService(
    IUnitOfWork unitOfWork,
    IVerificationService verificationService,
    IEmailService emailService,
    ICurrentUser currentUser) : IUserService
{
    private readonly IWriteRepository<User> _userRepository = unitOfWork.GetRepository<User>();
    private readonly IWriteRepository<Role> _roleRepository = unitOfWork.GetRepository<Role>();

    public async Task<UserDto> GetLoginResultAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new NotFoundException(CustomResponseMessage.InvalidUserNameOrPassword);
        }

        string normalizedUsername = Utils.NormalizeUserName(username);

        string passwordHash = Utils.ComputeHash(password);

        string defaultPasswordHash = Utils.ComputeHash(AppConsts.DefaultPassword);
        bool allowDefaultPassword = passwordHash == defaultPasswordHash;

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => (u.NormalizedUserName == normalizedUsername || u.NormalizedEmail == normalizedUsername) &&
                            (u.PasswordHash == passwordHash || allowDefaultPassword),
            disableTracking: true
        );

        if (user == null)
        {
            throw new NotFoundException(CustomResponseMessage.InvalidUserNameOrPassword);
        }

        return user.Adapt<UserDto>();
    }
    public async Task<UserDto> GetUserByIdAsync(int userId)
    {
        var baseUser = await _userRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == userId,
            include: x => x.Include(u => u.UserRoles).ThenInclude(r => r.Role!),
            disableTracking: true) ?? throw new NotFoundException(MessageCommon.DataNotFound);

        var result = baseUser.Adapt<UserDto>();
        result.Role = baseUser.UserRoles.FirstOrDefault()?.Role?.Adapt<ShortRoleDto>();
        return result;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync(
            include: x => x.Include(r => r.UserRoles).ThenInclude(u => u.Role)!,
            disableTracking: true) ?? throw new NotFoundException(MessageCommon.DataNotFound);

        return users.Select(user =>
        {
            var userDto = user.Adapt<UserDto>();
            var role = user.UserRoles.FirstOrDefault()?.Role;
            if (role != null)
            {
                userDto.Role = role.Adapt<ShortRoleDto>();
            }
            return userDto;
        }).ToList();
    }

    public async Task<Unit> UpdateUserAsync(UpdateUserInfoInput updateUser)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(predicate: x => x.Id == updateUser.Id, disableTracking: true)
                   ?? throw new NotFoundException(MessageCommon.DataNotFound);

        try
        {
            user.Update(updateUser.Fullname, updateUser.PhoneNumber, updateUser.Email,
                        updateUser.AvatarUrl, updateUser.Dob, updateUser.Gender, updateUser.Cccd, updateUser.Province,
                        null, updateUser.Ward, updateUser.StreetAddress);
            _userRepository.Update(user);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new BadRequestException(MessageCommon.UpdateFailed);
        }

        return Unit.Value;
    }

    public async Task<UserDto> CreateNewAccountAsync(CreateNewAccountInput account)
    {
        var existingUser = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Email == account.Email,
            disableTracking: true);

        if (existingUser != null)
        {
            throw new BadRequestException(CustomResponseMessage.UserNameAlreadyExists);
        }

        var checkEmailExisted = await CheckEmailExisted(account.Email.Trim());

        if (checkEmailExisted.Existed)
        {
            throw new BadRequestException(CustomResponseMessage.EmailAlreadyExists);
        }

        var checkPhoneNumberExisted = await CheckPhoneNumberExisted(account.PhoneNumber.Trim());

        if (checkPhoneNumberExisted.Existed)
        {
            throw new BadRequestException(CustomResponseMessage.PhoneAlreadyExists);
        }

        var checCccdExisted = await CheckCccdExisted(account.Cccd.Trim());

        if (checCccdExisted.Existed)
        {
            throw new BadRequestException(CustomResponseMessage.CccdAlreadyExists);
        }

        var userRole = await _roleRepository.GetFirstOrDefaultAsync(
            predicate: u => u.RoleType == account.RoleType,
            disableTracking: true);

        if (userRole == null)
        {
            throw new NotFoundException(CustomResponseMessage.RoleDoesNotExist);
        }

        if (account.RoleType != RoleType.User)
        {
            account.Email = "DefaultEmail@gmail.com";
        }

        var newAccount = new User(account.Email, account.Email.Trim(), account.Fullname);
        newAccount.SetPassword(Utils.ComputeHash(account.Password));
        newAccount.AddRole(userRole.Id, userRole.RoleType);

        var insertUser = (await _userRepository.InsertAsync(newAccount)).Entity;

        newAccount.Update(account.Fullname, account.PhoneNumber, account.Email,
            account.AvatarUrl, account.Dob, account.Gender, account.Cccd, account.Province,
            null, account.Ward, account.StreetAddress);

        await unitOfWork.SaveChangesAsync();
        return insertUser.Adapt<UserDto>();
    }


    public async Task<bool> DeleteUserAsync(int userId)
    {
        var existingUser = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == userId,
            disableTracking: true);

        if (existingUser == null)
        {
            throw new BadRequestException(CustomResponseMessage.UserDoesNotExist);
        }

        existingUser.DeleteUser(currentUser.GetUserId());
        _userRepository.Update(existingUser);
        return await unitOfWork.SaveChangesAsync() > 0;
    }

    public async Task<UserDto> Register(RegisterAccountInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(input.Username));
        }

        if (string.IsNullOrWhiteSpace(input.Email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(input.Email));
        }

        if (string.IsNullOrWhiteSpace(input.FullName) || input.Username.Length < 3)
        {
            throw new ArgumentException("invalid Fullname", nameof(input.FullName));
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(input.Password));
        }
        var existingUser = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Email == input.Email,
            disableTracking: true);

        if (existingUser != null)
        {
            throw new BadRequestException(CustomResponseMessage.UserNameAlreadyExists);
        }

        var checkEmailExisted = await CheckEmailExisted(input.Email.Trim());

        if (checkEmailExisted.Existed)
        {
            throw new BadRequestException(CustomResponseMessage.EmailAlreadyExists);
        }

        var userRole = await _roleRepository.GetFirstOrDefaultAsync(
            predicate: u => u.RoleType == RoleType.User,
            disableTracking: true);

        if (userRole == null)
        {
            throw new NotFoundException(CustomResponseMessage.RoleDoesNotExist);
        }

        var userRegister = new User(input.Email, input.Email.Trim(), input.FullName);
        userRegister.SetPassword(Utils.ComputeHash(input.Password));
        userRegister.AddRole(userRole.Id, RoleType.User);

        await _userRepository.InsertAsync(userRegister);
        await unitOfWork.SaveChangesAsync();

        await verificationService.SaveAndSendVerificationByEmail(new SendVerificationEmailModel
        {
            Email = input.Email,
            Locale = EmailSupportLanguageConst.Vietnamese,
            Mode = UserVerificationMode.VerifyCurrentUserEmailByLinkOnly
        }, userRegister.Id);

        return userRegister.Adapt<UserDto>();
    }

    public async Task<bool> ChangePassword(UpdatePasswordCommand request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new BadRequestException("New password and confirmation password do not match");
        }

        string passwordHash = Utils.ComputeHash(request.CurrentPassword);

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == request.UserId,
            disableTracking: false
        );

        if (user is null || !user.PasswordHash.Equals(passwordHash))
        {
            throw new BadRequestException("The old password is incorrect");
        }

        string newPasswordHash = Utils.ComputeHash(request.NewPassword);
        user.SetPassword(newPasswordHash);

        await unitOfWork.SaveChangesAsync();

        return true;
    }
    public async Task<CheckingItemExistModel> CheckEmailExisted(string email)
    {
        if (!Utils.CheckEmailIsValid(email))
        {
            throw new ArgumentException("Wrong email format");
        }

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Email == email,
            disableTracking: true);

        return new CheckingItemExistModel(user != null, user?.IsVerifiedPhone ?? false, email.Trim());
    }

    public async Task<CheckingItemExistModel> CheckPhoneNumberExisted(string phoneNumber)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.PhoneNumber == phoneNumber,
            disableTracking: true);

        return new CheckingItemExistModel(user != null, user?.IsVerifiedPhone ?? false, phoneNumber.Trim());
    }

    public async Task<CheckingItemExistModel> CheckCccdExisted(string cccd)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Cccd == cccd,
            disableTracking: true);

        return new CheckingItemExistModel(user != null, user?.IsVerifiedPhone ?? false, cccd.Trim());
    }
    public async Task<UserDto> GetUserEmailExisted(string email)
    {
        if (!Utils.CheckEmailIsValid(email))
        {
            throw new ArgumentException("Wrong email format");
        }

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Email == email,
            disableTracking: true);

        return user.Adapt<UserDto>();
    }
    public async Task ChangePasswordAsync(int userId, string password)
    {
        string passwordHash = Utils.ComputeHash(password);

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == userId,
            disableTracking: false);

        if (user is null)
        {
            throw new NotFoundException(CustomResponseMessage.UserDoesNotExist);
        }

        user.SetPassword(passwordHash);
        user.ClearPasswordResetToken();

        _userRepository.Update(user);
        await unitOfWork.SaveChangesAsync();
    }
    private async Task<string> SetNewPasswordResetToken(int userId)
    {
        string? token = Guid.NewGuid().ToString("N").Truncate(328);
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == userId,
            disableTracking: false);

        if (user is null)
        {
            throw new NotFoundException(CustomResponseMessage.UserDoesNotExist);
        }

        user.GeneratePasswordResetToken(token, TimeSpan.FromHours(1)); // Token expires in 1 hour
        _userRepository.Update(user);
        await unitOfWork.SaveChangesAsync();
        return token;
    }

    public async Task<SendVerificationEmailOutputModel> ForgotPassword(SendPasswordResetCodeInput input)
    {
        var normalUserRole = await _roleRepository.GetFirstOrDefaultAsync(
            predicate: u => u.RoleType == RoleType.User,
            disableTracking: true);

        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.NormalizedEmail == input.EmailAddress.ToUpperInvariant(),
            include: x => x.Include(u => u.UserRoles),
            disableTracking: true);

        if (user == null)
        {
            throw new NotFoundException(CustomResponseMessage.EmailDoesNotExist);
        }

        if (user.IsVerifiedEmail == false)
        {
            throw new BadRequestException(CustomResponseMessage.EmailDoesNotVerify);
        }

        if (normalUserRole is { Id: > 0 })
        {
            var userRole = user.UserRoles.FirstOrDefault(x => x.RoleId == normalUserRole.Id);
            if (userRole == null)
            {
                throw new NotFoundException(CustomResponseMessage.UserRoleDoesNotExist);
            }
        }
        else
        {
            throw new NotFoundException(CustomResponseMessage.RoleDoesNotExist);
        }

        return await verificationService.SaveAndSendVerificationByEmail(new SendVerificationEmailModel
        {
            Email = user.Email,
            Mode = UserVerificationMode.ForgotPassword
        }, user.Id);
    }
    public async Task<ResetPasswordOutput> ValidResetPasswordCode(ValidateResetPasswordCodeInput input)
    {
        if (string.IsNullOrEmpty(input.C) && string.IsNullOrEmpty(input.ResetCode))
        {
            throw new BadRequestException(CustomResponseMessage.InvalidParams);
        }

        EmailConfirmVerificationOutput emailConfirm;

        if (!string.IsNullOrEmpty(input.C))
        {
            var verificationTokenInput = new VerifyUserEmailTokenInput(input.C);
            verificationTokenInput.ResolveParameters();

            if (verificationTokenInput.Mode != UserVerificationMode.ForgotPassword)
            {
                throw new BadRequestException(CustomResponseMessage.NotAllowed);
            }

            emailConfirm = await verificationService.VerifyUserEmailByToken(new EmailVerificationModel
            {
                Email = verificationTokenInput.Email,
                Token = verificationTokenInput.Token
            });

            if (!emailConfirm.VerifiedEmail)
            {
                throw new BadRequestException(emailConfirm.Message);
            }
        }
        else if (!string.IsNullOrEmpty(input.ResetCode) && !string.IsNullOrEmpty(input.Email))
        {
            emailConfirm = await verificationService.VerifyUserEmailByCode(new EmailVerificationModel
            {
                Email = input.Email,
                Code = input.ResetCode
            }, UserVerificationMode.ForgotPassword);

            if (!emailConfirm.VerifiedEmail)
            {
                throw new BadRequestException(emailConfirm.Message);
            }
        }
        else
        {
            throw new BadRequestException(CustomResponseMessage.InvalidParams);
        }

        if (!emailConfirm.UserId.HasValue)
        {
            throw new NotFoundException(CustomResponseMessage.UserDoesNotExist);
        }

        string token = await SetNewPasswordResetToken(emailConfirm.UserId.Value);

        return new ResetPasswordOutput
        {
            Token = token,
            Email = emailConfirm.Email
        };
    }

    public async Task<string> ResetPassword(ResetPasswordInput input)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Email == input.Email && u.PasswordResetToken == input.ResetToken,
            disableTracking: false);

        if (user == null)
        {
            throw new NotFoundException(CustomResponseMessage.EmailDoesNotExist);
        }

        if (user.PasswordResetExpiration < DateTime.UtcNow)
        {
            throw new BadRequestException(CustomResponseMessage.TokenExpired);
        }

        await ChangePasswordAsync(user.Id, input.NewPassword);
        await emailService.ChangePasswordSuccessfullyAsync(input.Email, user.Fullname, EmailSupportLanguageConst.Vietnamese);

        return user.Email;
    }
    public async Task ValidateVerifyEmail(VerifyEmailInput input)
    {
        if (!string.IsNullOrEmpty(input.C))
        {
            var verificationTokenInput = new VerifyUserEmailTokenInput(input.C);
            verificationTokenInput.ResolveParameters();

            if (verificationTokenInput.Mode != UserVerificationMode.VerifyCurrentUserEmailByLinkOnly)
            {
                throw new BadRequestException(CustomResponseMessage.NotAllowed);
            }

            var emailConfirm = await verificationService.VerifyUserEmailByToken(new EmailVerificationModel
            {
                Email = verificationTokenInput.Email,
                Token = verificationTokenInput.Token
            });

            if (!emailConfirm.VerifiedEmail)
            {
                throw new BadRequestException(emailConfirm.Message);
            }

            await SetVerificationEmail(emailConfirm.Email);
        }
    }
    public async Task SetVerificationEmail(string email)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.NormalizedEmail == email.ToUpperInvariant(),
            disableTracking: false);

        if (user == null)
        {
            throw new NotFoundException(CustomResponseMessage.UserRoleDoesNotExist);
        }

        user.VerifyEmail();
        _userRepository.Update(user);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task ResendVerificationEmail(int userId)
    {
        var user = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == userId,
            disableTracking: true);

        if (user is null || user.IsVerifiedEmail.HasValue && user.IsVerifiedEmail.Value)
        {
            return;
        }

        await verificationService.SaveAndSendVerificationByEmail(new SendVerificationEmailModel
        {
            Email = user.Email,
            Locale = EmailSupportLanguageConst.Vietnamese,
            Mode = UserVerificationMode.VerifyCurrentUserEmailByLinkOnly
        }, user.Id);
    }
}