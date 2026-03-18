using Application.Dto.Authorization.Role;
using Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Persistence.Catalog.User;

public class UserDto : ShortUserInfo
{
    public DateOnly Dob { get; set; }
    public bool Gender { get; set; }
    public string Job { get; set; } = string.Empty;
    public string Cccd { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;

    public DateTime? LastLogin { get; set; }
    public bool Active { get; set; }
    public bool? IsVerifiedPhone { get; set; }
    public bool? IsVerifiedEmail { get; set; }
    public string? RegisterProvider { get; set; }

    public ShortRoleDto? Role { get; set; }
}

public class ShortUserInfo
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class SortUserInfoWithHospotal : ShortUserInfo
{
    public int StaffId { get; set; }
    public int? HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public ShortRoleDto? Role { get; set; }
}

public class HandleUserInfo : ShortUserInfo
{
    public ShortRoleDto? Role { get; set; }
}
public class UpdateUserInfoInput
{
    public int Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateOnly Dob { get; set; }
    public bool Gender { get; set; }
    public string Job { get; set; }
    public string Cccd { get; set; }
    public string Province { get; set; }
    public string Ward { get; set; }
    public string StreetAddress { get; set; }
}

public class CreateDoctorAcccountDto
{
    public string Fullname { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime Dob { get; set; }
    public bool Gender { get; set; }
    public string Job { get; set; } = "Bác Sĩ";
    public string Cccd { get; set; }
    public string Province { get; set; }
    public string Ward { get; set; }
    public string StreetAddress { get; set; }
}

public class CreateNewAccountInput
{
    public int? HospitalId { get; set; }
    public int? DepartmentId { get; set; }
    public RoleType RoleType { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateOnly Dob { get; set; }
    public bool Gender { get; set; }
    public string Job { get; set; }
    public string Cccd { get; set; }
    public string Province { get; set; }
    public string Ward { get; set; }
    public string StreetAddress { get; set; }
}

public class UpdateUserPasswordInput
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string RePassword { get; set; } = string.Empty;
}

public class UploadAvatarInput
{
    public IFormFile FileData { get; set; }
}