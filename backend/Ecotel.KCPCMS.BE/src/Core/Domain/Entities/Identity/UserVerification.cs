using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class UserVerification : AuditableEntity<int>, IAggregateRoot
{
    [MaxLength(256)]
    public string Mode { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset? TokenExpirationDate { get; set; }

    [MaxLength(15)]
    public string VerificationCode { get; set; } = string.Empty;

    public DateTimeOffset? CodeExpirationDate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }

    public int Status { get; set; }

    [MaxLength(500)]
    public string Link { get; set; } = string.Empty;

    public int? UserId { get; set; }

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    public DateTimeOffset DeletedDate { get; set; }
}