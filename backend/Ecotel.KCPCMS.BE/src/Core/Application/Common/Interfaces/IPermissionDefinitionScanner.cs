namespace Application.Common.Interfaces;

public interface IPermissionDefinitionScanner
{
    /// <summary>
    /// Các mã quyền đang được [HasPermission] trên controller kiểm tra; dùng để đối chiếu với AppPermissions.
    /// </summary>
    IReadOnlyCollection<string> ScanEnforcedCodes();
}
