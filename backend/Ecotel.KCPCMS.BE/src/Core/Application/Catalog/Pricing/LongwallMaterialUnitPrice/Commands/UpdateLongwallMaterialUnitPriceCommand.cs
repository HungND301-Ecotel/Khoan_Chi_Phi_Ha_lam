using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;

public record UpdateLongwallMaterialUnitPriceCommand(UpdateLongwallMaterialUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateLongwallMaterialUnitPriceCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICacheService cacheService) : IRequestHandler<UpdateLongwallMaterialUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(UpdateLongwallMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {

        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.Id != request.UpdateModel.Id &&
            m.StartMonth < request.UpdateModel.EndMonth &&
            m.EndMonth > request.UpdateModel.StartMonth &&
            m.LongwallParametersId == request.UpdateModel.LongwallParametersId &&
            m.CuttingThicknessId == request.UpdateModel.CuttingThicknessId &&
            m.SeamFaceId == request.UpdateModel.SeamFaceId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(m => m.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.MaterialUnitPriceNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, materialUnitPrice.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        bool longwallParamsTask = await _longwallParametersRepository.AnyAsync(p => p.Id == request.UpdateModel.LongwallParametersId);
        bool cuttingThicknessTask = await _cuttingThicknessRepository.AnyAsync(p => p.Id == request.UpdateModel.CuttingThicknessId);
        bool seamFaceTask = await _seamFaceRepository.AnyAsync(p => p.Id == request.UpdateModel.SeamFaceId);
        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.UpdateModel.ProcessId);

        bool technologyTask = true;
        if (request.UpdateModel.TechnologyId.HasValue)
        {
            technologyTask = await _technologyRepository.AnyAsync(p => p.Id == request.UpdateModel.TechnologyId.Value);
        }

        var checkData = longwallParamsTask && cuttingThicknessTask && seamFaceTask && processTask && technologyTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            materialUnitPrice.Update(
                request.UpdateModel.Code,
                request.UpdateModel.ProcessId,
                request.UpdateModel.LongwallParametersId,
                request.UpdateModel.CuttingThicknessId,
                request.UpdateModel.SeamFaceId,
                request.UpdateModel.TechnologyId,
                request.UpdateModel.StartMonth,
                request.UpdateModel.EndMonth,
                request.UpdateModel.TotalPrice
                );

            _materialUnitPriceRepository.Update(materialUnitPrice);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
    }
}
