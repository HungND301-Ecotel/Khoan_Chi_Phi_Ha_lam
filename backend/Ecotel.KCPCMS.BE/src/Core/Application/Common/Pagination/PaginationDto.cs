namespace Application.Common.Pagination;

public class PaginationDto
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalRow { get; set; }
}