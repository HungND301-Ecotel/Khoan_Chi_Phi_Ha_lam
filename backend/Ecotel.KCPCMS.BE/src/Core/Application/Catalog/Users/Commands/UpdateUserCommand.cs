using Application.Dto.Persistence.Catalog.User;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Users.Commands;

public record class UpdateUserCommand(UpdateUserInfoInput UpdateUser) : IRequest<Unit>;

public class UpdateUserCommandHandler(IUserService userService) : IRequestHandler<UpdateUserCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await userService.UpdateUserAsync(request.UpdateUser);
    }
}
