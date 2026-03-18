using Host.Controllers.Base;

namespace Host.Controllers.Catalog;

public class UserController : BaseAuthController
{
    //[HttpGet]
    //[CustomAuthorize(RoleType.SystemAdmin)]
    //public async Task<IActionResult> GetUsersAsync()
    //{
    //    var result = await Mediator.Send(new GetAllUsersQuery());
    //    return Ok(result, MessageCommon.GetDataSuccess);
    //}

    //[HttpGet("{userId:int}")]
    //public async Task<IActionResult> GetUserByIdAsync([FromRoute] int userId)
    //{
    //    var result = await Mediator.Send(new GetUserByIdQuery(userId));
    //    return Ok(result, MessageCommon.GetDataSuccess);
    //}

    //[HttpPost("create")]
    //[CustomAuthorize(RoleType.SystemAdmin)]
    //[OpenApiOperation("Create new account except for doctor account")]
    //public async Task<IActionResult> CreateUserAsync([FromBody] CreateNewAccountInput account)
    //{
    //    var result = await Mediator.Send(new CreateNewAccountCommand(account));
    //    return Ok(result, MessageCommon.CreateSuccess);
    //}

    //[HttpPut("Update")]
    //public async Task<IActionResult> UpdateUserAsync([FromBody] UpdateUserInfoInput updateUser)
    //{
    //    await Mediator.Send(new UpdateUserCommand(updateUser));
    //    return Ok(MessageCommon.UpdateSuccess);
    //}

    //[HttpDelete("{userId}")]
    //[CustomAuthorize(RoleType.SystemAdmin)]
    //[OpenApiOperation("delete user with id = userId")]
    //public async Task<IActionResult> DeleteUserAsync([FromRoute] int userId)
    //{
    //    await Mediator.Send(new DeleteUserCommand(userId));
    //    return Ok(MessageCommon.DeleteSuccess);
    //}

    //[HttpPost("change-password")]
    //[OpenApiOperation("Change password of current user")]
    //public async Task<IActionResult> ChangePasswordAsync([FromBody] UpdatePasswordCommand request)
    //{
    //    await Mediator.Send(request);
    //    return Ok(MessageCommon.UpdateSuccess);
    //}
}
