using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.OpenApi;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public class SwaggerHeaderAttribute(
    string headerName,
    string? description = null,
    string? defaultValue = null,
    bool isRequired = false)
    : Attribute
{
    public string HeaderName { get; } = headerName;
    public string? Description { get; } = description;
    public string? DefaultValue { get; } = defaultValue;
    public bool IsRequired { get; } = isRequired;
}