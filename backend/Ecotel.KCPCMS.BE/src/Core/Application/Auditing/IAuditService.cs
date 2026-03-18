using Application.Common.Interfaces;

namespace Application.Auditing;

public interface IAuditService : ITransientService
{
    Task<List<AuditDto>> GetUserTrailsAsync(long userId);
}