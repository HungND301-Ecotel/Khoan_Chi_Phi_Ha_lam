using System.Text.RegularExpressions;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;

public record CreateLongwallMaterialUnitPriceCommand(CreateLongwallMaterialUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateLongwallMaterialUnitPriceCommandHandler(
    IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateLongwallMaterialUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Power> _powerRepository = unitOfWork.GetRepository<Power>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

    // Valid format: "M =12m", "M =12,7m", "M =12.7m"
    private static readonly Regex InterpolationSeamFaceRegex =
        new(@"^M =\d+([,.]\d+)?m$", RegexOptions.Compiled);

    public async Task<bool> Handle(CreateLongwallMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        // --- Interpolation SeamFace handling ---
        // If InterpolationSeamFaceValue is provided, validate format and reuse an existing
        // SeamFace when present; otherwise create a new SeamFace and use its Id.
        var resolvedSeamFaceId = request.CreateModel.SeamFaceId;

        if (!string.IsNullOrEmpty(request.CreateModel.InterpolationSeamFaceValue))
        {
            var interpolationValue = request.CreateModel.InterpolationSeamFaceValue.Trim();

            if (!InterpolationSeamFaceRegex.IsMatch(interpolationValue))
            {
                throw new BadRequestException(
                    "Giá trị mặt vỉa nội suy không đúng định dạng. " +
                    "Định dạng hợp lệ: M =<số>m hoặc M =<số>,<số>m (ví dụ: M =12,7m).");
            }

            var existingSeamFace = await _seamFaceRepository.GetFirstOrDefaultAsync(
                predicate: s => s.Value == interpolationValue,
                disableTracking: true);

            if (existingSeamFace is not null)
            {
                resolvedSeamFaceId = existingSeamFace.Id;
            }
            else
            {
                var newSeamFace = SeamFace.Create(interpolationValue);
                await _seamFaceRepository.InsertAsync(newSeamFace, cancellationToken);
                resolvedSeamFaceId = newSeamFace.Id;
            }
        }

        if (!resolvedSeamFaceId.HasValue)
        {
            throw new BadRequestException("Mặt vỉa không được để trống.");
        }

        if (request.CreateModel.PowerId.HasValue && request.CreateModel.HardnessId.HasValue)
        {
            throw new BadRequestException("Chỉ được chọn một trong hai: Công suất hoặc Độ kiên cố than đá.");
        }
        if (!request.CreateModel.PowerId.HasValue && !request.CreateModel.HardnessId.HasValue)
        {
            throw new BadRequestException("Phải chọn Công suất hoặc Độ kiên cố than đá.");
        }

        // --- Month range overlap check (uses resolvedSeamFaceId) ---
        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.CreateModel.EndMonth &&
            m.EndMonth > request.CreateModel.StartMonth &&
            m.LongwallParametersId == request.CreateModel.LongwallParametersId &&
            m.CuttingThicknessId == request.CreateModel.CuttingThicknessId &&
            m.SeamFaceId == resolvedSeamFaceId.Value &&
            m.PowerId == request.CreateModel.PowerId &&
            m.HardnessId == request.CreateModel.HardnessId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        // --- Assignment code existence check ---
        var assignemntCodeIds = request.CreateModel.Costs.Select(c => c.AssignmentCodeId).Distinct();
        var assignmentCodeTask = await _assignmentCodeRepository.GetAllAsync(selector: a => a.Id, disableTracking: true);

        var checkExisted = assignemntCodeIds.All(id => assignmentCodeTask.Any(ac => ac == id));
        if (!checkExisted)
        {
            throw new Exception("Một hoặc nhiều Mã giao khoán không tồn tại.");
        }

        // --- Referenced entity existence checks ---
        bool longwallParamsTask = await _longwallParametersRepository.AnyAsync(p => p.Id == request.CreateModel.LongwallParametersId);
        bool cuttingThicknessTask = await _cuttingThicknessRepository.AnyAsync(p => p.Id == request.CreateModel.CuttingThicknessId);

        // SeamFace was already created above for interpolation path, so skip DB check in that case
        bool seamFaceTask = !string.IsNullOrEmpty(request.CreateModel.InterpolationSeamFaceValue)
            || await _seamFaceRepository.AnyAsync(p => p.Id == request.CreateModel.SeamFaceId!.Value);

        bool powerTask = true;
        if (request.CreateModel.PowerId.HasValue)
        {
            powerTask = await _powerRepository.AnyAsync(p => p.Id == request.CreateModel.PowerId.Value);
        }

        bool hardnessTask = true;
        if (request.CreateModel.HardnessId.HasValue)
        {
            hardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId.Value);
        }

        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProcessId);

        bool technologyTask = true;
        if (request.CreateModel.TechnologyId.HasValue)
        {
            technologyTask = await _technologyRepository.AnyAsync(p => p.Id == request.CreateModel.TechnologyId.Value);
        }

        var checkData = longwallParamsTask
            && cuttingThicknessTask
            && seamFaceTask
            && processTask
            && technologyTask
            && powerTask
            && hardnessTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var newMaterialUnitPrice = Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice.Create(
                request.CreateModel.Code,
                request.CreateModel.ProcessId,
                request.CreateModel.LongwallParametersId,
                request.CreateModel.CuttingThicknessId,
                resolvedSeamFaceId.Value,
                request.CreateModel.PowerId,
                request.CreateModel.HardnessId,
                request.CreateModel.PowerId.HasValue,
                request.CreateModel.TechnologyId,
                request.CreateModel.StartMonth,
                request.CreateModel.EndMonth,
                request.CreateModel.OtherMaterialValue,
                request.CreateModel.Costs.Adapt<List<MaterialUnitPriceAssignmentCode>>());

            await _materialUnitPriceRepository.InsertAsync(newMaterialUnitPrice, cancellationToken);
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
