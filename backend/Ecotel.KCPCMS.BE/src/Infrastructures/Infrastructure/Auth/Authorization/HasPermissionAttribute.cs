using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string code, string moduleName = "", string subModuleName = "")
        : base(PermissionPolicyProvider.Prefix + code)
    {
        Code = code;
        ModuleName = moduleName;
        SubModuleName = subModuleName;
    }

    public string Code { get; }

    public string ModuleName { get; }

    public string SubModuleName { get; }
}