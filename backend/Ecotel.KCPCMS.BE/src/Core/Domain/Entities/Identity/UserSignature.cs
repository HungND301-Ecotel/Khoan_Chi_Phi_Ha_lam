using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Common.Events;

namespace Domain.Entities.Identity;

public class UserSignature : AuditableEntity<Guid>, IAggregateRoot
{
    public int  UserId { get; protected set; }
    public SignatureType SignatureType { get; protected set; }
    public string? SignatureFile { get; protected set; }
    public string? CertificateId { get; protected set; }
    public string? CertificateFile { get; protected set; }
    public string? PinHash { get; protected set; }
    public bool IsPinSaved { get; protected set; } = false;
    public bool IsActive { get; protected set; } = true;

    public virtual User User { get; protected set; } = null!;

    // cho ký nháy ký thường 
    public static UserSignature Create(int  userId, SignatureType signatureType, string? signatureFile)
    {
        return new UserSignature
        {
            UserId = userId,
            SignatureType = signatureType,
            SignatureFile = signatureFile,
            IsPinSaved = false
        };
    }
    // cho ký số
    public static UserSignature CreateForDigital(int userId, string? certificateId, string? pinHash, bool isPinSaved)
    {
        return new UserSignature
        {
            UserId = userId,
            SignatureType = SignatureType.Digital,
            CertificateId = certificateId,
            PinHash = pinHash,
            IsPinSaved = isPinSaved,
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
}
