using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers.Base;

[ApiController]
public class BaseApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}