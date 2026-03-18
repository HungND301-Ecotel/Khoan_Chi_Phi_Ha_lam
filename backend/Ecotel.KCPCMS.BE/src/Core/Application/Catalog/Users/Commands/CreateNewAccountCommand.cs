using Application.Dto.Persistence.Catalog.User;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Users.Commands;

public record class CreateNewAccountCommand(CreateNewAccountInput Account) : IRequest<UserDto>;

public class CreateNewAccountCommandHandler(IUserService _userService) : IRequestHandler<CreateNewAccountCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateNewAccountCommand request, CancellationToken cancellationToken)
    {
        return await _userService.CreateNewAccountAsync(request.Account);
    }
}

