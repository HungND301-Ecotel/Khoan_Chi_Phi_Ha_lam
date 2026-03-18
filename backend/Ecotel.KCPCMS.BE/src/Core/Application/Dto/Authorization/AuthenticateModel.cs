using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization;

public class AuthenticateModel
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}