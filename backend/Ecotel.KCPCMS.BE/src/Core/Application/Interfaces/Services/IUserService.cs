using Application.Catalog.Users.Commands;
using Application.Dto.Authorization.Accounts;
using Application.Dto.Authorization.Verification;
using Application.Dto.Persistence.Catalog.User;
using MediatR;

namespace Application.Interfaces.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();

    Task<Unit> UpdateUserAsync(UpdateUserInfoInput updateUser);

    Task<UserDto> CreateNewAccountAsync(CreateNewAccountInput account);

    Task<bool> DeleteUserAsync(int userId);
    Task<UserDto> GetLoginResultAsync(string username, string password);
    Task<bool> ChangePassword(UpdatePasswordCommand request);
    Task<CheckingItemExistModel> CheckEmailExisted(string email);
    Task<UserDto> GetUserEmailExisted(string email);
    Task<UserDto> GetUserByIdAsync(int userId);
    Task<UserDto> Register(RegisterAccountInput input);
    Task ChangePasswordAsync(int userId, string password);
    Task<SendVerificationEmailOutputModel> ForgotPassword(SendPasswordResetCodeInput input);
    Task<ResetPasswordOutput> ValidResetPasswordCode(ValidateResetPasswordCodeInput input);
    Task<string> ResetPassword(ResetPasswordInput input);
    Task ValidateVerifyEmail(VerifyEmailInput input);
    Task SetVerificationEmail(string email);
    Task ResendVerificationEmail(int userId);
}