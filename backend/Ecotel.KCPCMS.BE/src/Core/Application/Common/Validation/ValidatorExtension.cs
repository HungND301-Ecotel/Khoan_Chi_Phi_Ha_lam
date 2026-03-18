using FluentValidation;
using FluentValidation.Results;

namespace Application.Common.Validation;

public static class ValidatorExtension
{
    public static IRuleBuilderOptions<T, TProperty> WithDuplicateMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, string field, string entityName)
    {
        return rule
            .WithName(field)
            .WithMessage($"{entityName} {field} is duplicated, please try again");
    }

    public static IRuleBuilderOptions<T, TProperty> WithDuplicateMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, string entityName)
    {
        return rule.WithMessage($"{entityName} is duplicated, please try again");
    }

    public static IRuleBuilderOptions<T, TProperty> WithDuplicateMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule)
    {
        return rule
            .WithMessage("This is duplicated, please try again.");
    }

    public static IRuleBuilderOptions<T, TProperty> WithDeleteInUsedMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule)
    {
        return rule
            .WithMessage("Record(s) in use. Cannot delete.");
    }

    public static void AddFailureWithId<T>(this ValidationContext<T> context, string field, long id)
    {
        string message = $"This is duplicated, please try again.{{{{{id}}}}}";

        var failure = new ValidationFailure($"{field}", message);
        context.AddFailure(failure);
    }

    public static void AddFailureWithId<T>(this ValidationContext<T> context, IEnumerable<string> fields, long id)
    {
        string message = $"This is duplicated, please try again.{{{{{id}}}}}";

        foreach (string field in fields)
        {
            var failure = new ValidationFailure($"{field}", message);
            context.AddFailure(failure);
        }
    }

    public static IRuleBuilderOptions<T, TProperty> WithRequiredMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule)
    {
        return rule
            .WithMessage("You must specify a value for this required field.");
    }

    public static IRuleBuilderOptions<T, TProperty> WithExceededCharactersMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, int number)
    {
        return rule.WithMessage($"You have exceeded {number} characters");
    }
}