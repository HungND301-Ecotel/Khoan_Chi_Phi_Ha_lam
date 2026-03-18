using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Metrics.Commands;
public record UpdateMetricCommand<TEntity>(MetricDto UpdateModel) : IRequest<bool>
    where TEntity : class;

public class UpdateMetricCommandHandler<TEntity>(IUnitOfWork unitOfWork) : IRequestHandler<UpdateMetricCommand<TEntity>, bool>
    where TEntity : class
{
    private readonly IWriteRepository<TEntity> _metricRepository = unitOfWork.GetRepository<TEntity>();
    public async Task<bool> Handle(UpdateMetricCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var idProperty = typeof(MetricDto).GetProperty("Id");
        if (idProperty == null)
        {
            throw new InvalidOperationException("UpdateModel does not have Id property");
        }

        var idValue = (DefaultIdType)idProperty.GetValue(request.UpdateModel)!;

        var existing = await _metricRepository.GetFirstOrDefaultAsync(
            predicate: e => EF.Property<DefaultIdType>(e, "Id") == idValue,
            disableTracking: true
        ) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        // Cập nhật: giả sử entity có method Update(...) hoặc bạn map DTO vào entity
        // Bạn có thể dùng Mapster để map các field từ DTO sang entity
        request.UpdateModel.Adapt(existing);

        _metricRepository.Update(existing);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}