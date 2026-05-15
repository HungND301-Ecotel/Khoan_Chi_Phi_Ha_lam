using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Commands;

public record UpdateTunnelSupportAndDrillingMaterialUnitPriceCommand(UpdateTunnelSupportAndDrillingMaterialUnitPrice UpdateModel) : IRequest<bool>;

public class UpdateTunnelSupportAndDrillingMaterialUnitPriceCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICacheService cacheService) : IRequestHandler<UpdateTunnelSupportAndDrillingMaterialUnitPriceCommand, bool>
{
    private readonly IWriteRepository<TunnelSupportAndDrillingMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelSupportAndDrillingMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<MaterialUnitPriceAssignmentCode>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "TunnelSupportAndDrillingMaterialUnitPrice";

    public async Task<bool> Handle(UpdateTunnelSupportAndDrillingMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.UpdateModel.EndMonth &&
            m.EndMonth > request.UpdateModel.StartMonth &&
            m.ProcessId == request.UpdateModel.ProcessId &&
            m.PassportId == request.UpdateModel.PassportId &&
            m.HardnessId == request.UpdateModel.HardnessId &&
            m.TechnologyId == request.UpdateModel.TechnologyId &&
            m.Id != request.UpdateModel.Id))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var assignemntCodeIds = request.UpdateModel.Costs.Select(c => c.AssignmentCodeId).Distinct();
        var assignmentCodeTask = await _assignmentCodeRepository.GetAllAsync(selector: a => a.Id, disableTracking: true);

        var checkExisted = assignemntCodeIds.All(id => assignmentCodeTask.Any(ac => ac == id));

        if (!checkExisted)
        {
            throw new Exception("Một hoặc nhiều Nhóm vật tư, tài sản không tồn tại.");
        }

        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(m => m.Code).Include(m => m.MaterialUnitPriceAssignmentCodes),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.MaterialUnitPriceNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, materialUnitPrice.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.UpdateModel.ProcessId);
        bool passportTask = await _passportRepository.AnyAsync(p => p.Id == request.UpdateModel.PassportId);
        bool hardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.UpdateModel.HardnessId);

        var checkData = processTask && passportTask && hardnessTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _materialUnitPriceAssignmentCodeRepository.Delete(materialUnitPrice.MaterialUnitPriceAssignmentCodes);
            materialUnitPrice.Update(
                request.UpdateModel.Code,
                request.UpdateModel.ProcessId,
                request.UpdateModel.PassportId,
                request.UpdateModel.HardnessId,
                request.UpdateModel.TechnologyId,
                request.UpdateModel.StartMonth,
                request.UpdateModel.EndMonth,
                request.UpdateModel.OtherMaterialValue,
                request.UpdateModel.Costs.Adapt<List<MaterialUnitPriceAssignmentCode>>()
                );

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);
            cacheService.InvalidateGroup(ModuleCacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
    }
}

