using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Catalog.Employee;

public class EmployeeDto : IDto, IAvatarCarrier
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string? PositionName { get; set; }
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateOnly Dob { get; set; }
    public string Cccd { get; set; }
    public string Avatar { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public bool Genre { get; set; }
    public bool IsActive { get; set; }


    [JsonIgnore]
    public string? AvatarKey => Avatar;

    public void SetAvatarUrl(string? url)
    {
        Avatar = url ?? string.Empty;
    }
}

public class CreateEmployeeDto
{
    public string FullName { get; set; } = string.Empty;    
    public int PositionId { get; set; }                     
    public Guid DepartmentId { get; set; }                
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Cccd { get; set; }
    public DateOnly Dob { get; set; }
    public bool Genre { get; set; }
}

public class UpdateEmployeeDto
{
    public string FullName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly? Dob { get; set; }
    public bool? Gender { get; set; }
    [MaxLength(255)]
    public string? Cccd { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }

}

public class ChangeEmployeeAvatarDto
{
    public int EmployeeId { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

public class ChangeEmployeePasswordDto
{
    public int EmployeeId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
public class UpdateEmployeeSignaturesDto
{
    public int EmployeeId { get; set; }
    public SignatureType SignatureType { get; set; }
    public string? SignatureFileUrl { get; set; } // initial , normal

    // Chỉ dùng khi SignatureType = Digital
    public string? CertificateId { get; set; }
    public string? PinHash { get; set; }
    public bool IsPinSaved { get; set; }
}
public class EmployeeSignatureResponseDto
{
    public Guid Id { get; set; }
    public SignatureType SignatureType { get; set; }
    
    public string? CertificateId { get; set; }
    public bool IsPinSaved { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeExcelDto
{
    public int? Id { get; set; }

    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;


    [Display(Name = "Chức vụ")]
    public string? PositionName { get; set; }


    [Display(Name = "Phòng ban")]
    public string? DepartmentName { get; set; }

    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "CCCD")]
    public string? Cccd { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateOnly? Dob { get; set; }

    [Display(Name = "Giới tính")]
    public string? GenderName { get; set; }
    [Display(Name = "Trạng thái")]
    public string? IsActiveName { get; set; }
}
public class UploadEmployeeImageRequest
{
    public List<IFormFile> Files { get; set; } = new();
}
