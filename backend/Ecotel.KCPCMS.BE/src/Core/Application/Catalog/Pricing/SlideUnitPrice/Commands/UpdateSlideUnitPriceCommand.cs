using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SlideUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.SlideUnitPrice.Commands;

public record UpdateSlideUnitPriceCommand(UopdateSlideUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateSlideUnitPriceCommandHandler(
    IUnitOfWork unitOfWork, ICodeService codeService, ICacheService cacheService) : IRequestHandler<UpdateSlideUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPriceAssignmentCode> _slideUnitPriceAssignmentCode = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPriceAssignmentCode>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "SlideUnitPrice";

    public async Task<bool> Handle(UpdateSlideUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var model = request.UpdateModel;

        var existedSlideUnitPrice = await _slideUnitPriceRepository.GetFirstOrDefaultAsync(
                                        predicate: s => s.Id == model.Id,
                                        include: s => s.Include(s => s.SlideUnitPriceAssignmentCodes).Include(s => s.Code),
                                        disableTracking: true)
                                    ?? throw new NotFoundException(CustomResponseMessage.SlideUnitPriceNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existedSlideUnitPrice.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.SlideUnitPriceCodeAlreadyExists);
        }

        if (await _slideUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.UpdateModel.EndMonth &&
            m.EndMonth > request.UpdateModel.StartMonth &&
            m.ProcessGroupId == request.UpdateModel.ProcessGroupId &&
            m.PassportId == request.UpdateModel.PassportId &&
            m.HardnessId == request.UpdateModel.HardnessId && m.Id != request.UpdateModel.Id))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        bool hardnessTask = await _hardnessRepository.AnyAsync(h => h.Id == model.HardnessId);
        bool passportTask = await _passportRepository.AnyAsync(h => h.Id == model.PassportId);
        bool processGroupTask = await _processGroupRepository.AnyAsync(h => h.Id == model.ProcessGroupId);

        var checkData = hardnessTask && passportTask && processGroupTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        var groupedCosts = model.Costs
            .GroupBy(c => c.AssignmentCodeId)
            .Select(g => new { AssignmentCodeId = g.Key, Costs = g.ToList() })
            .ToList();

        var assigmentCodeIds = request.UpdateModel.Costs.Select(c => c.AssignmentCodeId).Distinct().ToList();

        var assignmentList =
            await _assignmentCodeRepository.GetAllAsync(
                predicate: a => assigmentCodeIds.Contains(a.Id),
                include: a => a.Include(a => a.AssignmentCodeMaterials).ThenInclude(am => am.Material).ThenInclude(m => m.Costs),
                disableTracking: true);

        if (assigmentCodeIds.Count != assignmentList.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }
        var assignmentCodeMap = assignmentList.ToDictionary(a => a.Id, a => a);

        var unitPriceAssignmentCodes = new List<Domain.Entities.Pricing.SlideUnitPriceAssignmentCode>();

        foreach (var group in groupedCosts)
        {
            if (!assignmentCodeMap.TryGetValue(group.AssignmentCodeId, out var assCode))
            {
                throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
            }

            var assMaterials = assCode.AssignmentCodeMaterials
                .Where(link => link.Material != null)
                .Select(link => link.Material!)
                .ToList();
            var assMaterialIds = assMaterials.Select(m => m.Id).ToHashSet();
            var extraMaterialIds = group.Costs
                .Select(c => c.MaterialId)
                .Where(id => !assMaterialIds.Contains(id))
                .Distinct()
                .ToList();
            if (extraMaterialIds.Any())
            {
                throw new ConflictException(CustomResponseMessage.AssignmentCodeInvalidMaterialIds);
            }

            var duplicateIds = group.Costs
                .GroupBy(c => c.MaterialId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateIds.Any())
            {
                throw new BadRequestException(CustomResponseMessage.AssignmentCodeDuplicateMaterialIds);
            }

            foreach (var cost in group.Costs)
            {
                var material = assMaterials.FirstOrDefault(c => c.Id == cost.MaterialId);
                var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
                var curCost = material?.Costs.FirstOrDefault(c =>
                    c.StartMonth <= currentMonth && c.EndMonth >= currentMonth)?.Amount;
                unitPriceAssignmentCodes.Add(Domain.Entities.Pricing.SlideUnitPriceAssignmentCode.Create(group.AssignmentCodeId, material?.Id ?? DefaultIdType.Empty, cost.Amount ?? 0));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _slideUnitPriceAssignmentCode.Delete(existedSlideUnitPrice.SlideUnitPriceAssignmentCodes);

            existedSlideUnitPrice.Update(
                request.UpdateModel.Code,
                request.UpdateModel.ProcessGroupId,
                request.UpdateModel.HardnessId,
                request.UpdateModel.PassportId,
                request.UpdateModel.StartMonth,
                request.UpdateModel.EndMonth,
                unitPriceAssignmentCodes);

            _slideUnitPriceRepository.Update(existedSlideUnitPrice);

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

