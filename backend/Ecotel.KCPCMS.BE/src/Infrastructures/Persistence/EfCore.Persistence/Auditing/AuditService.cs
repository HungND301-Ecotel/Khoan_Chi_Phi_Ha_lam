using Application.Auditing;
using EfCore.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Persistence.Auditing;

public class AuditService(ApplicationDbContext context) : IAuditService
{
    public async Task<List<AuditDto>> GetUserTrailsAsync(long userId)
    {
        var trails = await context.AuditTrails
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DateTime)
            .Take(250)
            .ToListAsync();

        return trails.Adapt<List<AuditDto>>();
    }
}