using Application.Catalog.Index.Employee.Commands;
using Application.Catalog.Index.Employee.Queries;
using Application.Catalog.Users.Commands;
using Application.Catalog.Users.Queries;
using Application.Dto.Authorization.Accounts;
using Application.Dto.Authorization.Permission;
using Application.Dto.Catalog.Employee;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Common.Enums;
using Host.Controllers.Base;
using Infrastructure.Auth.Authorization;
using Infrastructure.Services.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Shared.Constants;

namespace Host.Controllers.Catalog;

public class UserController : BaseAuthController
{
    #region Employee

    [HttpGet("Employee")]
    [OpenApiOperation("Get All Employee", "")]
    [HasPermission("catalog.employee.read", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> GetAllEmployee(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = "",
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool ignorePagination = false)
    {
        var result = await Mediator.Send(new GetAllEmployeeQuery(pageIndex, pageSize, search, departmentId, ignorePagination));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Employee/{id:int}")]
    [OpenApiOperation("Get Employee by Id", "")]
    public async Task<IActionResult> GetEmployeeById([FromRoute] int id)
    {
        var result = await Mediator.Send(new GetEmployeeByIdQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPost("Employee")]
    [OpenApiOperation("Create Employee", "")]
    [HasPermission("catalog.employee.create","Danh mục", "Nhân viên")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        var result = await Mediator.Send(new CreateEmployeeCommand(dto));
        return Ok(result, MessageCommon.CreateSuccess);
    }

    [HttpPut("Employee/{id:int}")]
    [OpenApiOperation("Update Employee Info", "")]
    [HasPermission("catalog.employee.update", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> UpdateEmployeeInfo([FromRoute] int id, [FromBody] UpdateEmployeeDto dto)
    {

        var result = await Mediator.Send(new UpdateEmployeeCommand(id, dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPatch("Employee/avatar")] //nv tự dổi avatar của mình
    [OpenApiOperation("Change Employee Avatar", "")]
    public async Task<IActionResult> ChangeEmployeeAvatar([FromBody] ChangeEmployeeAvatarDto dto)
    {
        var result = await Mediator.Send(new ChangeEmployeeAvatarCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPatch("Employee/password")] // nv tự đổi mật khẩu của mình
    [OpenApiOperation("Change Employee Password", "")]
    public async Task<IActionResult> ChangeEmployeePassword([FromBody] ChangeEmployeePasswordDto dto)
    {
        var result = await Mediator.Send(new ChangeEmployeePasswordCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpDelete("Employee/{id:int}")]
    [OpenApiOperation("Delete Employee", "")]
    [HasPermission("catalog.employee.delete", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> DeleteEmployee([FromRoute] int id)
    {
        var result = await Mediator.Send(new DeleteEmployeeCommand(id));
        return Ok(result, MessageCommon.DeleteSuccess);
    }

    [HttpDelete("Employee/delete-list")]
    [OpenApiOperation("Delete Employee List", "")]
    [HasPermission("catalog.employee.delete", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> DeleteEmployeeList([FromBody] List<int> ids)
    {
        var result = await Mediator.Send(new DeleteEmployeeListCommand(ids));
        return Ok(result, MessageCommon.DeleteSuccess);
    }



    [HttpPost("Employee/{id}/reset-password")]
    [HasPermission("catalog.employee.update", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var result = await Mediator.Send(new ResetEmployeePasswordCommand(id));

        if (!result)
        {
            return BadRequest("Có lỗi xảy ra, không thể reset mật khẩu cho nhân viên này.");
        }

        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpGet("Employee/{employeeId:int}/signature")]
    [OpenApiOperation("Get Employee Signatures", "")]
    public async Task<IActionResult> GetEmployeeSignatures([FromRoute] int employeeId)
    {
        var result = await Mediator.Send(new GetEmployeeSignatureQuery(employeeId));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpGet("Employee/signatures/{signatureId:guid}/file")]
    [OpenApiOperation("Get current user signature file", "")]
    public async Task<IActionResult> GetMySignatureFile([FromRoute] Guid signatureId)
    {
        var result = await Mediator.Send(new GetMySignatureFileQuery(signatureId));
        return File(result.Data, result.ContentType, result.FileName);
    }

    [HttpGet("Employee/{id:int}/profile")]
    [OpenApiOperation("Get Employee Profile", "")]
    public async Task<IActionResult> GetMyProfile([FromRoute] int id)
    {
        var result = await Mediator.Send(new GetMyProfileQuery(id));
        return Ok(result, MessageCommon.GetDataSuccess);
    }

    [HttpPatch("Employee/{id:int}/lock")]
    [OpenApiOperation("Lock Employee Account", "")]
    [HasPermission("catalog.employee.update", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> SetEmployeeLock([FromRoute] int id, [FromQuery] bool isLocked)
    {
        var result = await Mediator.Send(new SetEmployeeLockCommand(id, isLocked));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpGet("verify-email")]
    [OpenApiOperation("Verify Email", "User click link trong email để xác thực")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string c)
    {
        var result = await Mediator.Send(new VerifyEmployeeEmailCommand(c));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPost("Employee/{id:int}/resend-verification")]
    [OpenApiOperation("Resend Verification Email", "")]
    //[HasPermission("catalog.employee.update", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> ResendVerificationEmail([FromRoute] int id)
    {
        var result = await Mediator.Send(new ResendEmployeeVerificationCommand(id));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpGet("Employee/export")]
    [OpenApiOperation("Export Employee List To Excel", "")]
    [HasPermission("catalog.employee.export", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> ExportEmployee()
    {
        var fileBytes = await Mediator.Send(new ExportExcelEmployeeQuery());
        var fileName = $"danh-sach-nhan-vien-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("Employee/import")]
    [Consumes("multipart/form-data")]
    [OpenApiOperation("Import Employee List From Excel", "")]
    [HasPermission("catalog.employee.import", "Danh mục", "Nhân viên")]
    public async Task<IActionResult> ImportEmployee([FromForm] ImportDto importModel)
    {
        var result = await Mediator.Send(new ImportExcelEmployeeCommand(importModel.FormFile));
        return Ok(result, MessageCommon.CreateSuccess);
    }
    #endregion

    #region Signature
    [HttpPost("Employee/upload-image")]
    [Consumes("multipart/form-data")]
    [OpenApiOperation("Upload Employee Image", "Dùng cho avatar, initialSignature, standardSignature")]
    public async Task<IActionResult> UploadEmployeeImage([FromForm] UploadEmployeeImageRequest request, [FromQuery] SignatureType? signatureType = null)
    {
        var folderPath = signatureType switch
        {
            SignatureType.Initial => "employee/signature/initial",
            SignatureType.Normal => "employee/signature/standard",
            //SignatureType.Digital => "employee/signature/digital",
            _ => "employee/avatar"
        };
        var result = await Mediator.Send(new UploadEmployeeImageCommand(request.Files, folderPath));
        return Ok(result, MessageCommon.UpdateSuccess);
    }

    [HttpPatch("Employee/signature")]
    [OpenApiOperation("Update Employee Signature", "")]
    public async Task<IActionResult> UpdateEmployeeSignature([FromBody] UpdateEmployeeSignaturesDto dto)
    {
        var result = await Mediator.Send(new UpdateEmployeeSignatureCommand(dto));
        return Ok(result, MessageCommon.UpdateSuccess);

    }
    #endregion
}