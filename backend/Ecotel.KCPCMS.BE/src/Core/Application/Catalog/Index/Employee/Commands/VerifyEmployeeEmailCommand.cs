using Application.Common.Exceptions;
using Application.Dto.Authorization.Accounts;
using Application.Dto.Authorization.Verification;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Catalog.Index.Employee.Commands;

public record VerifyEmployeeEmailCommand(string Token) : IRequest<bool>;

public class VerifyEmployeeEmailCommandHandler(IUserService userService)
    : IRequestHandler<VerifyEmployeeEmailCommand, bool>
{
    public async Task<bool> Handle(VerifyEmployeeEmailCommand request, CancellationToken cancellationToken)
    {
        await userService.ValidateVerifyEmail(new VerifyEmailInput { C = request.Token });
        return true;
    }
}