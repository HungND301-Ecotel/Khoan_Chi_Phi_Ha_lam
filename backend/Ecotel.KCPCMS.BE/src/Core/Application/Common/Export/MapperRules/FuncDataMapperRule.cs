namespace Application.Common.Export.MapperRules;

public class FuncDataMapperRule(Func<string, string> func, params string[] applyFor)
    : ExportMapperBaseRule(applyFor)
{
    private Func<string, string> Func { get; } = func;

    public override Func<string, string> Data => Func;
}