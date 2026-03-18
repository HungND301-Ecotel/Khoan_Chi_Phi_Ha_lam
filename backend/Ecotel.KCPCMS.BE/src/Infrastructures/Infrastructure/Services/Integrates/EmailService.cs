using Application.Dto.Authorization.Verification;
using Application.Interfaces.Infrastructures.Integrates.External.Service.Email;
using Infrastructure.Localization;
using Shared.Constants;
using Shared.Constants.EmailTemplate;

namespace Infrastructure.Services.Integrates;

public class EmailService(IEmailTemplateProvider emailTemplateProvider, IEmailSender emailSender)
    : IEmailService
{
    public async Task SendVerificationPasswordReset(SendVerificationByEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.ResetPassword, language);

        emailTemplate = emailTemplate.Replace("{USER}", input.UserName);

        if (!string.IsNullOrEmpty(input.Code) && !string.IsNullOrEmpty(input.Token))
        {
            emailTemplate = emailTemplate.Replace("{CODE}", input.Code);
            emailTemplate = emailTemplate.Replace("{LINK}", input.Link);
        }

        await ReplaceBodyAndSend(input.Email, L.T("EMAIL_REQUEST_CHANGE_PASSWORD_TITLE", language), emailTemplate);
    }
    public async Task SendVerificationEmailVerify(SendVerificationByEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.CurrentEmail, language);

        if (!string.IsNullOrEmpty(input.Code))
        {
            emailTemplate = emailTemplate.Replace("{USER}", input.UserName);
            emailTemplate = emailTemplate.Replace("{CODE}", input.Code);
            emailTemplate = emailTemplate.Replace("{EMAIL}", input.Email);
        }

        await ReplaceBodyAndSend(input.Email, L.T("EMAIL_VERIFICATION_TITLE", language), emailTemplate);
    }
    public async Task SendVerificationEmailVerifyLinkOnly(SendVerificationByEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.CurrentEmailOnlyLink, language);

        if (!string.IsNullOrEmpty(input.Code) && !string.IsNullOrEmpty(input.Token))
        {
            emailTemplate = emailTemplate.Replace("{USER}", input.UserName);
            emailTemplate = emailTemplate.Replace("{LINK}", input.Link);
            emailTemplate = emailTemplate.Replace("{EMAIL}", input.Email);
        }

        await ReplaceBodyAndSend(input.Email, L.T("EMAIL_VERIFICATION_TITLE", language), emailTemplate);
    }
    public async Task ChangePasswordSuccessfullyAsync(string emailAddress, string username, string language = EmailSupportLanguageConst.Vietnamese)
    {
        if (string.IsNullOrEmpty(emailAddress))
        { return; }

        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.ChangePwdSuccess, language);

        emailTemplate = emailTemplate.Replace("{USER}", username);
        emailTemplate = emailTemplate.Replace("{EMAIL}", emailAddress);

        await ReplaceBodyAndSend(emailAddress, L.T("EMAIL_CHANGED_PASSWORD_SUCCESSFULLY", language), emailTemplate);
    }
    public async Task SendVerificationCodeForUpdateProfileAsync(SendVerificationByEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        if (string.IsNullOrEmpty(input.Email) || !input.Email.Contains("@"))
        {
            return;
        }


        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.VerifyEmailForUpdateInfo, language);

        emailTemplate = emailTemplate.Replace("{USER}", input.UserName);
        emailTemplate = emailTemplate.Replace("{CODE}", input.Code);

        await ReplaceBodyAndSend(input.Email, L.T("EMAIL_REQUEST_CHANGE_IN_PROFILE_TITLE", language), emailTemplate);
    }
    public async Task SendUserInvitationAsync(SendInviteUserEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.UserInvitation, language);
        string title = L.T("EMAIL_INVITE_USER_TITLE", language);
        title = title.Replace("{InviteeName}", input.InviteeName);
        emailTemplate = emailTemplate.Replace("{TITLE}", title);
        emailTemplate = emailTemplate.Replace("{InviteeName}", input.InviteeName);
        emailTemplate = emailTemplate.Replace("{InviteeEmail}", input.InviteeEmail);
        emailTemplate = emailTemplate.Replace("{OrganizationName}", input.OrganizationName);

        if (!string.IsNullOrEmpty(input.Link))
        {
            emailTemplate = emailTemplate.Replace("{LINK}", input.Link);
        }

        await ReplaceBodyAndSend(input.Email, L.T("EMAIL_INVITE_USER_SUBJECT", language), emailTemplate);
    }
    public async Task SendVerificationCodeAsync(SendVerificationByEmailInput input, string language = EmailSupportLanguageConst.Vietnamese)
    {
        string emailTemplate = await emailTemplateProvider.GetTemplateByNameAsync(EmailTemplateNameConst.LoginEmailCode, language);

        emailTemplate = emailTemplate.Replace("{EMAIL}", input.Email);

        switch (input.Mode)
        {
            case UserVerificationMode.VerificationForSignin:
                emailTemplate = emailTemplate.Replace("{TITLE}", L.T("EMAIL_LOGIN_CODE_TITLE", language));
                break;

            default:
                emailTemplate = emailTemplate.Replace("{TITLE}", L.T("EMAIL_VERIFICATION_TITLE", language));
                break;
        }

        if (!string.IsNullOrEmpty(input.Code))
        {
            emailTemplate = emailTemplate.Replace("{CODE}", input.Code);
        }

        switch (input.Mode)
        {
            case UserVerificationMode.VerificationForSignin:
                await ReplaceBodyAndSend(input.Email, L.T("EMAIL_LOGIN_CODE_TITLE", language), emailTemplate);
                break;

            default:
                await ReplaceBodyAndSend(input.Email, L.T("EMAIL_VERIFICATION_TITLE", language), emailTemplate);
                break;
        }
    }

    private async Task ReplaceBodyAndSend(string emailAddress, string subject, string emailTemplate)
    {
        await emailSender.SendAsync(emailAddress, subject, emailTemplate);
    }
}