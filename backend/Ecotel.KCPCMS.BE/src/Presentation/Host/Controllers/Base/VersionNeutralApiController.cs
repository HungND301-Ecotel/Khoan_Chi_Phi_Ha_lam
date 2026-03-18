using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers.Base;

[Route("api/[controller]")]
[ApiVersionNeutral]
public class VersionNeutralApiController : BaseApiController;