using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization.Accounts;

public class UpdatePasswordInput
{
    [Required]
    [MinLength(3)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string NewPassword { get; set; } = string.Empty;
}