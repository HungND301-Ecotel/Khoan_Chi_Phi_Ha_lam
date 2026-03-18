using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization.Accounts;

public class ResetPasswordInput
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(328)]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string NewPassword { get; set; } = string.Empty;
}