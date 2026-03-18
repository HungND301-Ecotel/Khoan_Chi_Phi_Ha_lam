using Application.Dto.Persistence.Catalog.User;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Users.Queries;

public record class GetAllUsersQuery() : IRequest<List<UserDto>>;

public class GetAllUsersQueryHandler(IUserService _userService) : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetAllUsersAsync();
    }
}
