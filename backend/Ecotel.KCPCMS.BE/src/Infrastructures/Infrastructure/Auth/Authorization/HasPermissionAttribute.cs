using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth.Authorization;

/// <summary>
/// Yêu cầu quyền <paramref name="code"/>, luôn truyền hằng số trong <c>Shared.Authorization.AppPermissions</c>.
/// <paramref name="screen"/> chỉ để tra cứu: ghi tên màn hình dùng API khi khác tên resource, không ảnh hưởng phân quyền.
/// </summary>
public class HasPermissionAttribute(string code, string screen = "")
    : AuthorizeAttribute(PermissionPolicyProvider.Prefix + code)
{
    public string Code { get; } = code;

    public string Screen { get; } = screen;
}
