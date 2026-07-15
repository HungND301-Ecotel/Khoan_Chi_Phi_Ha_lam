using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IPermissionCacheService
{
    Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    Task InvalidateUserAsync(int userId, CancellationToken cancellationToken = default);

    Task InvalidateUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default);
}
