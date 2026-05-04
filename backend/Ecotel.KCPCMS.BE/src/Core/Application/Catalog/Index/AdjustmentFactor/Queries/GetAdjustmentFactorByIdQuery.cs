using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record GetAdjustmentFactorByIdQuery(DefaultIdType Id) : IRequest<AdjustmentFactorDto>;

public class GetAdjustmentFactorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdjustmentFactorByIdQuery, AdjustmentFactorDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();

    public async Task<AdjustmentFactorDto> Handle(GetAdjustmentFactorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var adjustmentFactor = await _adjustmentFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q.Include(x => x.FixedKey).Include(x => x.ProcessGroup).Include(x => x.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var fixedKeyKey = adjustmentFactor.FixedKey?.Key ?? adjustmentFactor.Code?.Value ?? string.Empty;
        var fixedKeyType = adjustmentFactor.FixedKey?.Type ?? FixedKeyType.None;

        return new AdjustmentFactorDto
        {
            Id = adjustmentFactor.Id,
            Code = fixedKeyKey,
            FixedKeyId = adjustmentFactor.FixedKeyId,
            FixedKeyKey = fixedKeyKey,
            FixedKeyType = fixedKeyType,
            Name = adjustmentFactor.Name,
            Type = fixedKeyType.ToAdjustmentFactorType(),
            ProcessGroupId = adjustmentFactor.ProcessGroupId,
            ProcessGroupCode = adjustmentFactor.ProcessGroup?.FixedKey?.Key ?? adjustmentFactor.ProcessGroup?.Code?.Value ?? string.Empty,
            ProcessGroupName = adjustmentFactor.ProcessGroup?.Name ?? string.Empty,
        };
    }
}
