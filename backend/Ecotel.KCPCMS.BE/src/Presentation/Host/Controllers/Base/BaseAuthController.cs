using Application.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers.Base;

[Authorize]
public class BaseAuthController : VersionedApiController
{
    protected OkObjectResult Ok(object value, string message) => Ok(new ResponseBase<object>(value, message));
    protected OkObjectResult Ok<T>(T result, string message) => Ok(new ResponseBase<T>(result, message));
}

[Route("api/v{version:apiVersion}/admin/[controller]")]
public class BaseAdminAuthController : BaseAuthController;

[Route("api/v{version:apiVersion}/web/[controller]")]
public class BaseWebAuthController : BaseAuthController;

[Route("api/v{version:apiVersion}/mobile/[controller]")]
public class BaseMobileAuthController : BaseAuthController;