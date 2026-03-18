using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Authorization.Accounts;

public class VerifyEmailInput
{ 
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(328)]
    public string VerifyCode { get; set; } = string.Empty;

    [StringLength(1000)]
    public string C { get; set; } = string.Empty;
}