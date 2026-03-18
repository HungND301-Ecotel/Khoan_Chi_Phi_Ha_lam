using Microsoft.AspNetCore.Mvc;

namespace Host.Controllers.Base;

[Route("api/v{version:apiVersion}/[controller]")]
public class VersionedApiController : BaseApiController;