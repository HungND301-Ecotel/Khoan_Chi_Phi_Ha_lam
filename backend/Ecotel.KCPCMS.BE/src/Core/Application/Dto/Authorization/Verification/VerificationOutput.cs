namespace Application.Dto.Authorization.Verification;

public class VerificationOutput
{
    public string SessionInfo { get; set; } = string.Empty;
}

public class PhoneConfirmVerificationOutput
{
    public string Phone { get; set; } = string.Empty;
    public bool VerifiedPhone { get; set; }
    public long UpdatedDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
}

public class SendVerificationEmailOutputModel
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Sent { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class EmailConfirmVerificationOutput
{
    public string Email { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public bool VerifiedEmail { get; set; }
    public long UpdatedDate { get; set; }
    public string Message { get; set; } = string.Empty;
}