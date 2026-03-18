namespace Application.Common.Export.MapperRules;

public abstract class ExportMapperBaseRule(params string[] applyFor)
{
    public string[] ApplyFor { get; init; } = applyFor;

    public abstract Func<string, string> Data { get; }
}