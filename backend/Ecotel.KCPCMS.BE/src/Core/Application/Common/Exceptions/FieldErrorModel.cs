namespace Application.Common.Exceptions;

public class FieldErrorModel
{
    public FieldErrorModel(string fieldName, string rule, string message)
    {
        FieldName = fieldName;
        Rule = rule;
        Message = message;
    }

    public FieldErrorModel(string fieldName, string rule, string message, object? data)
    {
        FieldName = fieldName;
        Rule = rule;
        Message = message;
        Data = data;
    }

    public string FieldName { get; set; }
    public string? Rule { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}