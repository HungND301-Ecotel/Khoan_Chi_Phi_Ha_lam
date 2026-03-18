namespace Application.Common.Export.MapperRules;

public class BooleanMapperRule(string trueMapValue, string falseMapValue, params string[] applyFor)
    : ExportMapperBaseRule(applyFor)
{
    public override Func<string, string> Data => fieldValue => fieldValue switch
    {
        "True" => trueMapValue,
        "False" => falseMapValue,
        _ => fieldValue
    };
}