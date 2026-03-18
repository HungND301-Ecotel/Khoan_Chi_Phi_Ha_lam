using Application.Common.Caching;
using Domain.Common.Contracts;
using Infrastructure.Common.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Infrastructure.Caching;

public class CacheKeyService : ICacheKeyService
{
    private static string HashAlgorithm => "SHA1";

    public string GetCacheKey(string prefix, object id)
    {
        return $"{prefix}-{id}";
    }

    public static string GetCacheKey(string prefix, params object[]? prefixParameters)
    {
        return prefixParameters?.Any() ?? false
            ? string.Format(prefix, prefixParameters.Select(CreateCacheKeyParameters).ToArray())
            : prefix;
    }

    [ExcludeFromCodeCoverage]
    private static object CreateCacheKeyParameters(object parameter)
    {
        return parameter switch
        {
            null => "null",
            IEnumerable<int> ids => CreateIdsHash(ids),
            IEnumerable<BaseEntity> entities => CreateIdsHash(entities.Select(entity => entity.Id)),
            BaseEntity entity => entity.Id,
            decimal param => param.ToString(CultureInfo.InvariantCulture),
            _ => parameter
        };
    }

    private static string CreateIdsHash(IEnumerable<int> ids)
    {
        var identifiers = ids.ToList();

        if (!identifiers.Any())
        {
            return string.Empty;
        }

        string identifiersString = string.Join(", ", identifiers.OrderBy(id => id));
        return HashHelper.CreateHash(Encoding.UTF8.GetBytes(identifiersString), HashAlgorithm);
    }

    private static string CreateIdsHash(IEnumerable<Guid> ids)
    {
        var identifiers = ids.ToList();

        if (!identifiers.Any())
        {
            return string.Empty;
        }

        string identifiersString = string.Join(", ", identifiers.OrderBy(id => id));
        return HashHelper.CreateHash(Encoding.UTF8.GetBytes(identifiersString), HashAlgorithm);
    }
}