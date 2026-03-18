namespace Application.Common.Models;

public interface ISearch
{
    public List<string> Fields { get; set; }
    public string? Keyword { get; set; }
}