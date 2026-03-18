namespace Application.Common.Export.MapperRules;

public class DictionaryDataMapperRule(string from, string to, params string[] applyFor)
    : ExportMapperBaseRule(applyFor)
{
    public override Func<string, string> Data => fieldValue => from == fieldValue ? to : fieldValue;
}