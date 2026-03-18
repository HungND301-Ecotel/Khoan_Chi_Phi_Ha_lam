using Application.Common.Cqrs.Commands;
using FluentValidation;

namespace Application.Common.Validation;

public class DeleteValidator<TCommand, TId> : CustomValidator<TCommand> where TCommand : DeleteBaseCommand<TId>
{
    protected DeleteValidator()
    {
        CommonRules();
    }

    private void CommonRules()
    {
        RuleFor(c => c.Ids)
            .NotEmpty();
        RuleForEach(c => c.Ids)
            .NotEmpty();
    }
}

public class DeleteValidator<TCommand> : DeleteValidator<TCommand, long> where TCommand : DeleteBaseCommand<long>
{
    protected DeleteValidator()
    {
        CommonRules();
    }

    private void CommonRules()
    {
        RuleForEach(c => c.Ids)
            .GreaterThan(0);
    }
}