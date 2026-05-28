using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Commands;

public record CreateTunnelSupportAndDrillingMaterialUnitPriceCommand(CreateTunnelSupportAndDrillingMaterialUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateTunnelSupportAndDrillingMaterialUnitPriceCommandHandler(
    IUnitOfWork unitOfWork, ICodeService codeService, ICacheService cacheService) : IRequestHandler<CreateTunnelSupportAndDrillingMaterialUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "TunnelSupportAndDrillingMaterialUnitPrice";
    private readonly IWriteRepository<TunnelSupportAndDrillingMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelSupportAndDrillingMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    public async Task<bool> Handle(CreateTunnelSupportAndDrillingMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.CreateModel.EndMonth &&
            m.EndMonth > request.CreateModel.StartMonth &&
            m.ProcessId == request.CreateModel.ProcessId &&
            m.PassportId == request.CreateModel.PassportId &&
            m.HardnessId == request.CreateModel.HardnessId &&
            m.TechnologyId == request.CreateModel.TechnologyId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var assignemntCodeIds = request.CreateModel.Costs.Select(c => c.AssignmentCodeId).Distinct();
        var assignmentCodeTask = await _assignmentCodeRepository.GetAllAsync(selector: a => a.Id, disableTracking: true);

        var checkExisted = assignemntCodeIds.All(id => assignmentCodeTask.Any(ac => ac == id));

        if (!checkExisted)
        {
            throw new Exception("Một hoặc nhiều Nhóm vật tư, tài sản không tồn tại.");
        }

        if (request.CreateModel.Costs.Any(cost => cost.MaterialId == null || cost.MaterialId == Guid.Empty))
        {
            throw new BadRequestException(CustomResponseMessage.MaterialNotFound);
        }

        var duplicateRows = request.CreateModel.Costs
            .GroupBy(cost => new { cost.AssignmentCodeId, cost.MaterialId })
            .Any(group => group.Count() > 1);
        if (duplicateRows)
        {
            throw new BadRequestException("Dữ liệu vật tư theo nhóm bị trùng lặp.");
        }

        var materialIds = request.CreateModel.Costs
            .Select(cost => cost.MaterialId!.Value)
            .Distinct()
            .ToList();
        var existingMaterialIds = await _materialRepository.GetAllAsync(
            predicate: material => materialIds.Contains(material.Id),
            selector: material => material.Id,
            disableTracking: true);
        if (existingMaterialIds.Count != materialIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
        }

        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProcessId);
        bool passportTask = await _passportRepository.AnyAsync(p => p.Id == request.CreateModel.PassportId);
        bool hardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId);

        var checkData = processTask && passportTask && hardnessTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var newMaterialUnitPrice = TunnelSupportAndDrillingMaterialUnitPrice.Create(
                request.CreateModel.Code,
                request.CreateModel.ProcessId,
                request.CreateModel.PassportId,
                request.CreateModel.HardnessId,
                request.CreateModel.TechnologyId,
                request.CreateModel.StartMonth,
                request.CreateModel.EndMonth,
                request.CreateModel.OtherMaterialValue,
                request.CreateModel.Costs
                    .Select(cost => MaterialUnitPriceAssignmentCode.Create(
                        cost.AssignmentCodeId,
                        cost.TotalPrice,
                        cost.MaterialId,
                        cost.Norm))
                    .ToList());

            await _materialUnitPriceRepository.InsertAsync(newMaterialUnitPrice, cancellationToken);
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

