using Application.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers.Base;

[AllowAnonymous]
public class BaseNoAuthController : VersionedApiController
{
    protected OkObjectResult Ok(object value, string message) => Ok(new ResponseBase<object>(value, message));
    protected OkObjectResult Ok<T>(T result, string message) => Ok(new ResponseBase<T>(result, message));
}