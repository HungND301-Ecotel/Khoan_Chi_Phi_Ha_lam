namespace Application.Common.Models;

public class Search : ISearch
{
    public List<string> Fields { get; set; } = [];
    public string? Keyword { get; set; }
}