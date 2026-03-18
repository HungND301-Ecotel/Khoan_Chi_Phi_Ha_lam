using Application.Dto.Authorization.Verification;

namespace Application.Interfaces.Infrastructures.Integrates.External.Service.Email;

public interface IEmailService
{
    Task ChangePasswordSuccessfullyAsync(string emailAddress, string fullName, string language);

    Task SendVerificationPasswordReset(SendVerificationByEmailInput input, string language);

    Task SendVerificationCodeAsync(SendVerificationByEmailInput input, string language);

    Task SendVerificationEmailVerify(SendVerificationByEmailInput input, string language);

    Task SendVerificationEmailVerifyLinkOnly(SendVerificationByEmailInput input, string language);

    Task SendVerificationCodeForUpdateProfileAsync(SendVerificationByEmailInput input, string language);
}