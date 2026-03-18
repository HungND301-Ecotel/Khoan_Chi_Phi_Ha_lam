using Shared.Constants.EmailTemplate;
using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Integrates.Emails;

public class SendVerificationByEmailInput
{
    public string Locale { get; set; } = EmailSupportLanguageConst.English;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string BaseClientUrl { get; set; } = string.Empty;
}