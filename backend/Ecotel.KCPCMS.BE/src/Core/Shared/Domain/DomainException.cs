namespace Shared.Domain;

public abstract class DomainException(string rule, string? fieldName, object? specs, string message)
    : Exception(message)
{
    public string Rule { get; protected set; } = rule;
    public string? FieldName { get; protected set; } = fieldName;
    public object? Specs { get; protected set; } = specs;
}