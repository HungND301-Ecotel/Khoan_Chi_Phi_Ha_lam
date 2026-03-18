using System.ComponentModel.DataAnnotations;

namespace Application.Common.Pagination;

public class PagedInputDto
{
    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 15;

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }
}