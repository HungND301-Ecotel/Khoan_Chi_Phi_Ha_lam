namespace Application.Common.Models;

// TODO: Split into 3 seperated interfaces
public interface IAdvancedFilter<TSearch, TFilter> : ISpecPayload
    where TSearch : ISearch
    where TFilter : IFilter<TFilter>
{
    public TSearch? AdvancedSearch { get; set; }
    public TFilter? AdvancedFilter { get; set; }
}

public interface IAdvancedFilter : IAdvancedFilter<Search, Filter>;