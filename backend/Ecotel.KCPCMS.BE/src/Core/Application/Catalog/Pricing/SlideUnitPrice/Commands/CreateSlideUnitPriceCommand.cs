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

public record CreateSlideUnitPriceCommand(CreateSlideUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateSLideUnitPriceCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateSlideUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

    public async Task<bool> Handle(CreateSlideUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var model = request.CreateModel;
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.SlideUnitPriceCodeAlreadyExists);
        }

        if (await _slideUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.CreateModel.EndMonth &&
            m.EndMonth > request.CreateModel.StartMonth &&
            m.ProcessGroupId == request.CreateModel.ProcessGroupId &&
            m.PassportId == request.CreateModel.PassportId &&
            m.HardnessId == request.CreateModel.HardnessId))
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
        var assigmentCodeIds = request.CreateModel.Costs.Select(c => c.AssignmentCodeId).Distinct().ToList();

        var assignmentList =
            await _assignmentCodeRepository.GetAllAsync(
                predicate: a => assigmentCodeIds.Contains(a.Id),
                include: a => a.Include(a => a.Materials).ThenInclude(m => m.Costs),
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

            var assMaterialIds = assCode.Materials.Select(m => m.Id).ToHashSet();
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
                var material = assCode.Materials.FirstOrDefault(c => c.Id == cost.MaterialId);
                var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
                var curCost = material?.Costs.FirstOrDefault(c =>
                    c.StartMonth <= currentMonth && c.EndMonth >= currentMonth)?.Amount;
                unitPriceAssignmentCodes.Add(Domain.Entities.Pricing.SlideUnitPriceAssignmentCode.Create(material?.Id ?? DefaultIdType.Empty, cost.Amount ?? 0));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newSlideUnitPrice = Domain.Entities.Pricing.SlideUnitPrice.Create(
                request.CreateModel.Code,
                request.CreateModel.ProcessGroupId,
                request.CreateModel.HardnessId,
                request.CreateModel.PassportId,
                request.CreateModel.StartMonth,
                request.CreateModel.EndMonth,
                unitPriceAssignmentCodes);

            await _slideUnitPriceRepository.InsertAsync(newSlideUnitPrice, cancellationToken);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}
