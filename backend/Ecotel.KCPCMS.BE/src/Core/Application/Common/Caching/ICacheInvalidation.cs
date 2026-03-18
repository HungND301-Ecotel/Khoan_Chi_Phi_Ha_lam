namespace Application.Common.Caching;

public interface ICacheInvalidation
{
    List<string> GetCacheKeysToInvalidate();
}
