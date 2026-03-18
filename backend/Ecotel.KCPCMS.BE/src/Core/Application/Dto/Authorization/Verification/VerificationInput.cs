using System.ComponentModel.DataAnnotations;
using Application.Utility;

namespace Application.Dto.Authorization.Verification;

public class SendVerificationInput
{
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public string CaptchaToken { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}

public class PhoneVerificationInput
{
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public string SessionInfo { get; set; } = string.Empty;
}

public class SendVerificationEmailInput
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;
}

public class SendVerificationEmailModel
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;
}

public class SendVerificationByEmailInput
{
    public string Mode { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}

public class SendInviteUserEmailInput : SendVerificationByEmailInput
{
    public string InviteeName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string InviteeEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class EmailVerificationInput
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
}

public class EmailVerificationModel
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}

public class PhoneVerificationModel
{
    public string Phone { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}

public class VerifyCurrentUserEmailTokenInput
{
    public string C { get; set; } = string.Empty;
}

public class VerifyCurrentUserEmailInput
{
    public string C { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
}

public class VerifyUserEmailTokenInput
{
    public string C { get; set; }

    public string Mode { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }

    public VerifyUserEmailTokenInput(string c)
    {
        this.C = c;
        Email = string.Empty;
        Token = string.Empty;
        Mode = string.Empty;
    }

    public virtual void ResolveParameters()
    {
        C = Uri.UnescapeDataString(C);
        if (string.IsNullOrEmpty(C))
        {
            throw new Exception("Invalid parameters");
        }

        string? parameterDecryptString = Utils.DecryptString(C);

        string[]? parameters = parameterDecryptString.Split("|");

        if (parameters.Length != 3)
        {
            throw new Exception("Invalid parameters");
        }

        Email = parameters[0];
        Token = parameters[1];
        Mode = parameters[2];
    }
}