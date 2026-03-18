using Application.Common.Validation;
using FluentValidation;

namespace Application.Identity.Tokens;

public record TokenRequest(string Username, string Password);

public class TokenRequestValidator : CustomValidator<TokenRequest>
{
    public TokenRequestValidator()
    {
        RuleFor(p => p.Username).Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Invalid Username.");

        RuleFor(p => p.Password).Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}