#pragma warning disable CA1716

namespace Application.Common.Models;

public static class FilterOperator
{
    public const string Eq = "eq";
    public const string Neq = "neq";
    public const string Lt = "lt";
    public const string Lte = "lte";
    public const string Gt = "gt";
    public const string Gte = "gte";
    public const string StartsWith = "startswith";
    public const string EndsWith = "endswith";
    public const string Contains = "contains";
    public const string In = "in";
    public const string NotContains = "notcontains";
}

public static class FilterLogic
{
    public const string And = "and";
    public const string Or = "or";
    public const string Xor = "xor";
}

public class Filter : IFilter<Filter>
{
    public string? Logic { get; set; }
    public IEnumerable<Filter>? Filters { get; set; }
    public string? Field { get; set; }
    public string? Operator { get; set; }
    public object? Value { get; set; }
}

public interface IFilter<TFilter> where TFilter : IFilter<TFilter>
{
    public string? Logic { get; set; }
    public IEnumerable<TFilter>? Filters { get; set; }
    public string? Field { get; set; }
    public string? Operator { get; set; }
    public object? Value { get; set; }
}