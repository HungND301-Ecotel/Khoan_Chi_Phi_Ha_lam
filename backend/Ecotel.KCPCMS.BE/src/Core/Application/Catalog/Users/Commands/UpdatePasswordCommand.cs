using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Common.Responses;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Catalog.Users.Commands;

public class UpdatePasswordCommand : IRequest<ResponseBase<bool>>
{
    [JsonIgnore]
    public long UserId { get; protected set; }

    public void SetUserId(long userId) => UserId = userId;

    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}

public class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
{
    public UpdatePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("New password is required.");
        RuleFor(x => x.ConfirmNewPassword).NotEmpty().WithMessage("Confirm new password is required.");
    }
}

public class UpdatePasswordCommandHandler(IUserService userService, ICurrentUser currentUser) : IRequestHandler<UpdatePasswordCommand, ResponseBase<bool>>
{
    public async Task<ResponseBase<bool>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        request.SetUserId(currentUser.UserId);
        bool result = await userService.ChangePassword(request);

        return new ResponseBase<bool>(result);
    }
}