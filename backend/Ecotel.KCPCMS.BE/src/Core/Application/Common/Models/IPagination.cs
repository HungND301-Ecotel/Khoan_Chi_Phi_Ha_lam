namespace Application.Common.Models;

public interface IPagination : ISpecPayload
{
    int PageNumber { get; set; }
    int PageSize { get; set; }
    bool IgnorePagination { get; set; }
}