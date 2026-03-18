using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization.Accounts;

public class ResetPasswordOutput
{
    [Required]
    [MaxLength(328)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}