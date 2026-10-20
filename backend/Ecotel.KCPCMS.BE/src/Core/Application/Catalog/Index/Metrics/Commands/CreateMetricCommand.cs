using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Commands;
public record CreateMetricCommand<TEntity>(CreateMetricDto CreateModel) : IRequest<bool>
    where TEntity : class;

public class CreateMetricCommandHandler<TEntity>(IUnitOfWork unitOfWork) : IRequestHandler<CreateMetricCommand<TEntity>, bool>
    where TEntity : class
{
    private readonly IWriteRepository<TEntity> _metricRepository = unitOfWork.GetRepository<TEntity>();
    public async Task<bool> Handle(CreateMetricCommand<TEntity> request, CancellationToken cancellationToken)
    {
        request.CreateModel.Note = string.IsNullOrWhiteSpace(request.CreateModel.Note)
            ? null
            : request.CreateModel.Note.Trim();

        var newEntity = request.CreateModel.Adapt<TEntity>();

        await _metricRepository.InsertAsync(newEntity);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}