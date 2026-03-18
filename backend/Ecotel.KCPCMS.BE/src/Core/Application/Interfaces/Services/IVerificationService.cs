using Application.Dto.Authorization.Verification;

namespace Application.Interfaces.Services;

public interface IVerificationService
{
    Task<SendVerificationEmailOutputModel> SaveAndSendVerificationByEmail(SendVerificationEmailModel input,
        int? userId = null);

    Task<EmailConfirmVerificationOutput> VerifyUserEmailByCode(EmailVerificationModel input, string mode);

    Task<EmailConfirmVerificationOutput> VerifyUserEmailByToken(EmailVerificationModel input);

    Task<EmailConfirmVerificationOutput> VerifyCurrentUserEmail(VerifyCurrentUserEmailInput input, string mode);

    Task<bool> HasSendVerificationCurrentUserEmail(string email);

    EmailVerificationModel ResolveParameters(string t);

    string NormalizeUrlToken(string token, string mode, string email);

    string EncryptToken(string email, string tokenCode, string mode);

    string NormalizeLocale(string locale);
}