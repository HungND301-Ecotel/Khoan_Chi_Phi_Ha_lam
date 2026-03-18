using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.StoneClampRatio.Queries;

public record GetStoneClampRatioByIdQuery(DefaultIdType Id) : IRequest<StoneClampRatioDto>;

public class GetStoneClampRatioByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetStoneClampRatioByIdQuery, StoneClampRatioDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    public async Task<StoneClampRatioDto> Handle(GetStoneClampRatioByIdQuery request, CancellationToken cancellationToken)
    {
        var stoneClampRatio = await _stoneClampRatioRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(t => t.Hardness).Include(t => t.ProductionProcess).ThenInclude(p => p.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new StoneClampRatioDto
        {
            Id = stoneClampRatio.Id,
            CoefficientValue = stoneClampRatio.CoefficientValue,
            HardnessId = stoneClampRatio.HardnessId,
            HardnessValue = stoneClampRatio.Hardness?.Value ?? "",
            ProcessCode = stoneClampRatio.ProductionProcess?.Code?.Value ?? "",
            ProcessId = stoneClampRatio.ProcessId,
            ProcessName = stoneClampRatio.ProductionProcess?.Code?.Value ?? "",
            Value = stoneClampRatio.Value,
        };
    }
}
