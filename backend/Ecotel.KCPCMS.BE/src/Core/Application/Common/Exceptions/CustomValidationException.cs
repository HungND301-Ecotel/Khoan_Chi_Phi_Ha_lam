namespace Application.Common.Exceptions;

public class CustomValidationException : AppLayerException
{
    public CustomValidationException()
    {
    }

    public CustomValidationException(FieldErrorModel error)
    {
        AddError(error);
    }

    public List<FieldErrorModel> Errors { get; set; } = [];

    public CustomValidationException AddError(FieldErrorModel error)
    {
        Errors.Add(error);
        return this;
    }

    public CustomValidationException AddErrors(IEnumerable<FieldErrorModel> errors)
    {
        Errors.AddRange(errors);
        return this;
    }
}