using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Common.Events;
using Domain.Entities.Identity;
using Domain.Events;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Employee : AuditableEntity<int>, IAggregateRoot
{
    [MaxLength(50)]
    public string FullName { get; private set; }

    [MaxLength(256)]
    public string Avatar { get; private set; } = string.Empty;

    public DateOnly? Dob { get; private set; }
    public bool? Gender { get; private set; }
    [MaxLength(255)]
    public string? Cccd { get; private set; }

    // Navigation Properties
    public int PositionId { get; private set; }
    public virtual Position? Position { get; private set; }

    public Guid DepartmentId { get; private set; }
    public virtual Department? Department { get; private set; }

    public int UserId { get; private set; }
    public virtual User? User { get; private set; }

    public Employee()
    {
        
    }


    public static Employee Create(string fullName,int userId, int positionId, Guid departmentId, string avatarUrl, DateOnly? dob,
                        bool? gender, string cccd)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(cccd))
        {
            throw new ArgumentException("CCCD không được để trống!");
        }

        return new Employee
        {
            FullName = fullName,
            UserId = userId,
            PositionId = positionId,
            DepartmentId = departmentId,
            Avatar = avatarUrl,
            Dob = dob,
            Gender = gender,
            Cccd = cccd
        };
    }

    public void UpdateEmployee(string fullName, int positionId, Guid departmentId, string avatarUrl, DateOnly? dob,
                        bool? gender, string cccd)
    {
        UpdateName(fullName);
        SetPosition(positionId);
        SetDepartment(departmentId);
        SetAvatar(avatarUrl);
        SetDob(dob);
        SetGender(gender);
        SetCccd(cccd);
    }

    



    #region Constructors
    

    public void SetPosition(int positionId)
    {
        PositionId = positionId;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetDepartment(Guid departmentId)
    {
        DepartmentId = departmentId;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
     
    public void SetAvatar(string? avatarUrl)
    {
        Avatar = avatarUrl ?? string.Empty;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void UpdateName(string? fullName)
    {
        FullName = fullName ?? string.Empty;

        // Add domain event for profile update
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
        DomainEvents.Add(new SyncUserInfoEvent(UserId));
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

    #endregion

}
