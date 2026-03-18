using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization.Accounts;

public class SendPasswordResetCodeInput
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;
}