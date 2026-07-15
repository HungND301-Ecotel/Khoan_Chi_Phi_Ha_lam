using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Catalog.Permissions;

public interface IPermissionCatalogSynchronizer
{
    Task SynchronizeAsync(CancellationToken cancellationToken = default);
}
