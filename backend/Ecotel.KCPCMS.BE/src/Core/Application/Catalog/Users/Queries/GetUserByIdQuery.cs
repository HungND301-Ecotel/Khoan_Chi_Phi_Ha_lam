using Application.Dto.Persistence.Catalog.User;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Users.Queries;
public record class GetUserByIdQuery(int UserId) : IRequest<UserDto>;

public class GetUserByIdQueryHandler(IUserService _userService) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetUserByIdAsync(request.UserId);
    }
}