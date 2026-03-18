using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionProcess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProductionProcess.Queries;
public record GetProductionProcessByIdQuery(DefaultIdType Id) : IRequest<ProductionProcessDto>;

public class GetProductionProcessByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductionProcessByIdQuery, ProductionProcessDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    public async Task<ProductionProcessDto> Handle(GetProductionProcessByIdQuery request, CancellationToken cancellationToken)
    {
        var productionProcess = await _productionProcessRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(p => p.ProcessGroup).Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return new ProductionProcessDto
        {
            Id = productionProcess.Id,
            Code = productionProcess.Code.Value,
            Name = productionProcess.Name,
            ProcessGroupId = productionProcess.ProcessGroupId,
            ProcessGroupName = productionProcess.ProcessGroup != null ? productionProcess.ProcessGroup.Name : string.Empty
        };
    }
}
