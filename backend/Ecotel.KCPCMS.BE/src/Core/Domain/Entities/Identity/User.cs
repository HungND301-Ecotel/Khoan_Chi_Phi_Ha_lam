using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Common.Events;
using Domain.Events;

namespace Domain.Entities.Identity;

public class User : AuditableEntity<int>, IAggregateRoot
{
    // Main constructor with required fields
    public User(string userName, string email, string? fullname)
    {
        SetUserName(userName);
        SetEmail(email);
        Fullname = fullname ?? string.Empty;
        JoinDate = DateTimeOffset.UtcNow;
        LockoutEnabled = true;
        AccessFailedCount = 0;

        // Add domain event for entity creation
        DomainEvents.Add(EntityCreatedEvent.WithEntity(this));
    }

    public User()
    {

    }

    [MaxLength(50)]
    public string UserName { get; private set; } = string.Empty;

    [MaxLength(50)]
    public string NormalizedUserName { get; private set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; private set; } = string.Empty;

    [MaxLength(256)]
    public string Avatar { get; private set; } = string.Empty;

    [MaxLength(256)]
    public string NormalizedEmail { get; private set; } = string.Empty;

    [MaxLength(500)]
    public string PasswordHash { get; private set; } = string.Empty;

    [MaxLength(15)]
    public string PhoneNumber { get; private set; } = string.Empty;

    public bool LockoutEnabled { get; private set; }

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public DateTimeOffset JoinDate { get; private set; }

    [MaxLength(50)]
    public string Fullname { get; private set; }

    public bool? IsVerifiedPhone { get; private set; }

    public bool? IsVerifiedEmail { get; private set; }

    [MaxLength(256)]
    public string PasswordResetToken { get; private set; } = string.Empty;

    public DateTimeOffset PasswordResetExpiration { get; private set; }

    [MaxLength(50)]
    public string? RegisterProvider { get; private set; }

    [MaxLength(255)]
    public string? Province { get; private set; }

    [MaxLength(255)]
    public string? District { get; private set; }

    [MaxLength(255)]
    public string? Ward { get; private set; }

    [MaxLength(255)]
    public string? StreetAddress { get; private set; }

    public DateOnly? Dob { get; private set; }
    public bool? Gender { get; private set; }
    [MaxLength(255)]
    public string? Cccd { get; private set; }

    // Navigation properties

    private readonly IList<UserRole> _userRoles = new List<UserRole>();
    public virtual IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // Domain methods - all state changes go through these methods
    public void Update(string fullname, string phonenumber,
                        string email, string avatarUrl, DateOnly? dob,
                        bool? gender, string cccd, string province, string? district,
                        string ward, string streetAddress)
    {
        UpdateName(fullname);
        SetPhoneNumber(phonenumber);
        SetEmail(email);
        SetAvatar(avatarUrl);
        SetDob(dob);
        SetGender(gender);
        SetCccd(cccd);
        SetProvince(province);
        SetDistrict(district ?? string.Empty);
        SetWard(ward);
        SetStreetAddress(streetAddress);
    }

    public void DeleteUser(int deleteBy = 0)
    {
        DeletedOn = DateTimeOffset.Now;
        DeletedBy = deleteBy;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void AddRole(int roleId, RoleType roleType)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
        {
            return; // Role already exists, do nothing
        }

        var userRole = new UserRole
        {
            UserId = Id,
            RoleId = roleId,
            RoleType = roleType
        };

        _userRoles.Add(userRole);
    }

    #region Constructors
    public void SetUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Username cannot be empty", nameof(userName));
        }

        if (userName.Length < 3)
        {
            throw new ArgumentException("Username must be at least 3 characters", nameof(userName));
        }

        UserName = userName;
        NormalizedUserName = userName.ToUpperInvariant();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        // Basic email validation using regex
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }

        Email = email;
        NormalizedEmail = email.ToUpperInvariant();

        // Reset verification when email changes
        IsVerifiedEmail = false;
    }

    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        DomainEvents.Add(new UserPasswordChangedEvent(this));
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            // Basic phone number validation - can be enhanced
            if (!Regex.IsMatch(phoneNumber.Trim(), @"^\+?[0-9]{10,15}$"))
            {
                throw new ArgumentException("Invalid phone number format", nameof(phoneNumber));
            }
        }

        PhoneNumber = phoneNumber;

        // Reset verification when phone changes
        IsVerifiedPhone = false;
    }

    public void SetAvatar(string? avatarUrl)
    {
        Avatar = avatarUrl ?? string.Empty;
    }

    public void UpdateName(string? fullname)
    {
        Fullname = fullname ?? string.Empty;

        // Add domain event for profile update
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
        DomainEvents.Add(new SyncUserInfoEvent(Id));
    }

    public void VerifyEmail()
    {
        IsVerifiedEmail = true;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
        DomainEvents.Add(new UserEmailVerifiedEvent(this));
    }

    public void VerifyPhone()
    {
        IsVerifiedPhone = true;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
        DomainEvents.Add(new UserPhoneVerifiedEvent(this));
    }

    public void LockAccount(TimeSpan duration)
    {
        LockoutEnd = DateTimeOffset.UtcNow.Add(duration);
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
        DomainEvents.Add(new UserLockedOutEvent(this, duration));
    }

    public void UnlockAccount()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public bool IsLockedOut()
    {
        return LockoutEnabled && LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
    }

    public void IncrementAccessFailedCount()
    {
        AccessFailedCount++;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void GeneratePasswordResetToken(string token, TimeSpan expiration)
    {
        PasswordResetToken = token;
        PasswordResetExpiration = DateTimeOffset.UtcNow.Add(expiration);
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = string.Empty;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public bool IsPasswordResetTokenValid()
    {
        return !string.IsNullOrEmpty(PasswordResetToken) &&
               PasswordResetExpiration > DateTimeOffset.UtcNow;
    }

    public void SetRegisterProvider(string provider)
    {
        RegisterProvider = provider;
    }

    public void SetDob(DateOnly? dob)
    {
        Dob = dob;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetGender(bool? gender)
    {
        Gender = gender;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetCccd(string cccd)
    {
        Cccd = cccd;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetProvince(string province)
    {
        Province = province;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetDistrict(string district)
    {
        District = district;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetWard(string ward)
    {
        Ward = ward;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetStreetAddress(string streetAddress)
    {
        StreetAddress = streetAddress;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    #endregion
}