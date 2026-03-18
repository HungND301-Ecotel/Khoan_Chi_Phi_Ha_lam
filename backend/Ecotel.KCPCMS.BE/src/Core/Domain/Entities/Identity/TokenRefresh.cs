using System.ComponentModel.DataAnnotations;
using Domain.Common.Contracts;

namespace Domain.Entities.Identity;

public class RefreshToken : BaseEntity<int>, IAggregateRoot
{
    public int UserId { get; protected set; }
    public int Type { get; protected set; }

    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiredDate { get; set; }

    public static RefreshToken Create(int userId, string refreshToken, DateTimeOffset expiredDate)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiredDate = expiredDate
        };
    }
}