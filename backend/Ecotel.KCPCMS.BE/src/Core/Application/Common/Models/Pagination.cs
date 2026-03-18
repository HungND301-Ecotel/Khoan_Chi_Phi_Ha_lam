namespace Application.Common.Models;

public class Pagination : IPagination, IOrderBy
{
    private int _pageNumber;

    private int _pageSize;

    public int PageNumber
    {
        get { return _pageNumber <= 0 ? 1 : _pageNumber; }
        set { _pageNumber = value; }
    }

    public int PageSize
    {
        get { return _pageSize <= 0 ? 10 : _pageSize; }
        set { _pageSize = value == 0 ? int.MaxValue : value; }
    }

    public string[]? OrderBy { get; set; }

    public bool HasOrderBy() => OrderBy?.Any() is true;

    public bool IgnorePagination { get; set; } = false;

    public bool ForceUseCustomOrderBy { get; set; } = false;
}