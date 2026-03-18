namespace Application.Common.Export.MapperRules;

public class DateTimeMapperRule(string format, params string[] applyFor)
    : FuncDataMapperRule(DateTimeMapperFunc(format), applyFor)
{
    private static readonly Func<string, Func<string, string>> DateTimeMapperFunc =
        format => s => DateTime.TryParse(s, out var dt) ? dt.ToString(format) : s;
}