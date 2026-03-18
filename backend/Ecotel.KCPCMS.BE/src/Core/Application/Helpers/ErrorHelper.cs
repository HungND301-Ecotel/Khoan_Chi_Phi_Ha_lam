using Application.Common.Exceptions;
using FluentValidation.Results;

namespace Application.Helpers;

public class ErrorHelper
{
    private const string ErrorCodePostfix = "Validator";

    public static CustomValidationException GenerateGeneralAppException(ValidationResult result)
    {
        var exception = new CustomValidationException();
        foreach (var error in result.Errors)
        {
            string errorCode = string.IsNullOrEmpty(error.ErrorCode)
                ? string.Empty
                : error.ErrorCode[..^ErrorCodePostfix.Length];
            exception.AddError(new FieldErrorModel(error.PropertyName, errorCode, error.ErrorMessage,
                error.AttemptedValue));
        }

        return exception;
    }

    public static CustomValidationException GenerateGeneralAppException(List<ValidationFailure> results)
    {
        var exception = new CustomValidationException();
        foreach (var error in results)
        {
            string errorCode = string.IsNullOrEmpty(error.ErrorCode)
                ? string.Empty
                : error.ErrorCode[..^ErrorCodePostfix.Length];

            string[] properties = error.PropertyName.Split(",");
            foreach (string property in properties)
            {
                exception.AddError(new FieldErrorModel(property.Trim(), errorCode, error.ErrorMessage,
                    error.AttemptedValue));
            }
        }

        return exception;
    }

    public static CustomValidationException GenerateGeneralAppException(string property, string rule,
        string errorMessage, object? data)
    {
        var exception = new CustomValidationException();
        exception.AddError(new FieldErrorModel(property, rule, errorMessage, data));
        return exception;
    }
}