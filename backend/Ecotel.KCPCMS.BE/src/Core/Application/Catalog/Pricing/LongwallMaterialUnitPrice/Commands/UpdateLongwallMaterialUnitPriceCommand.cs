using System.Text.RegularExpressions;
using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Mapster;
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
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Power> _powerRepository = unitOfWork.GetRepository<Power>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<MaterialUnitPriceAssignmentCode>();

    private const string ProductUnitPriceCacheSignalKey = "ProductUnitPrice";
    private const string LongwallMaterialUnitPriceCacheSignalKey = "LongwallMaterialUnitPrice";
    private static readonly Regex InterpolationSeamFaceRegex =
        new(@"^M =\d+([,.]\d+)?m$", RegexOptions.Compiled);

    public async Task<bool> Handle(UpdateLongwallMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(m => m.Code).Include(m => m.MaterialUnitPriceAssignmentCodes),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.MaterialUnitPriceNotFound);

        var resolvedSeamFaceId = request.UpdateModel.SeamFaceId;

        if (!string.IsNullOrEmpty(request.UpdateModel.InterpolationSeamFaceValue))
        {
            var interpolationValue = request.UpdateModel.InterpolationSeamFaceValue.Trim();

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
                if (existingSeamFace.Id != materialUnitPrice.SeamFaceId)
                {
                    throw new ConflictException(
                        $"Mặt vỉa với giá trị \"{interpolationValue}\" đã tồn tại trong hệ thống.");
                }

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

        if (request.UpdateModel.PowerId.HasValue && request.UpdateModel.HardnessId.HasValue)
        {
            throw new BadRequestException("Chỉ được chọn một trong hai: Công suất hoặc Độ kiên cố than đá.");
        }
        if (!request.UpdateModel.PowerId.HasValue && !request.UpdateModel.HardnessId.HasValue)
        {
            throw new BadRequestException("Phải chọn Công suất hoặc Độ kiên cố than đá.");
        }

        var a = await _materialUnitPriceRepository.GetAllAsync(predicate: m =>
            m.Id != request.UpdateModel.Id &&
            m.StartMonth < request.UpdateModel.EndMonth &&
            m.EndMonth > request.UpdateModel.StartMonth &&
            m.LongwallParametersId == request.UpdateModel.LongwallParametersId &&
            m.CuttingThicknessId == request.UpdateModel.CuttingThicknessId &&
            m.SeamFaceId == resolvedSeamFaceId.Value &&
            m.PowerId == request.UpdateModel.PowerId &&
            m.HardnessId == request.UpdateModel.HardnessId,
            disableTracking: true);

        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.Id != request.UpdateModel.Id &&
            m.StartMonth < request.UpdateModel.EndMonth &&
            m.EndMonth > request.UpdateModel.StartMonth &&
            m.LongwallParametersId == request.UpdateModel.LongwallParametersId &&
            m.CuttingThicknessId == request.UpdateModel.CuttingThicknessId &&
            m.SeamFaceId == resolvedSeamFaceId.Value &&
            m.PowerId == request.UpdateModel.PowerId &&
            m.HardnessId == request.UpdateModel.HardnessId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var assignemntCodeIds = request.UpdateModel.Costs.Select(c => c.AssignmentCodeId).Distinct();
        var assignmentCodeTask = await _assignmentCodeRepository.GetAllAsync(selector: a => a.Id, disableTracking: true);

        var checkExisted = assignemntCodeIds.All(id => assignmentCodeTask.Any(ac => ac == id));

        if (!checkExisted)
        {
            throw new Exception("Một hoặc nhiều Mã giao khoán không tồn tại.");
        }

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, materialUnitPrice.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        bool longwallParamsTask = await _longwallParametersRepository.AnyAsync(p => p.Id == request.UpdateModel.LongwallParametersId);
        bool cuttingThicknessTask = await _cuttingThicknessRepository.AnyAsync(p => p.Id == request.UpdateModel.CuttingThicknessId);
        bool seamFaceTask = await _seamFaceRepository.AnyAsync(p => p.Id == resolvedSeamFaceId.Value);
        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.UpdateModel.ProcessId);

        bool powerTask = true;
        if (request.UpdateModel.PowerId.HasValue)
        {
            powerTask = await _powerRepository.AnyAsync(p => p.Id == request.UpdateModel.PowerId.Value);
        }

        bool hardnessTask = true;
        if (request.UpdateModel.HardnessId.HasValue)
        {
            hardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.UpdateModel.HardnessId.Value);
        }

        bool technologyTask = true;
        if (request.UpdateModel.TechnologyId.HasValue)
        {
            technologyTask = await _technologyRepository.AnyAsync(p => p.Id == request.UpdateModel.TechnologyId.Value);
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
            _materialUnitPriceAssignmentCodeRepository.Delete(materialUnitPrice.MaterialUnitPriceAssignmentCodes);
            materialUnitPrice.Update(
                request.UpdateModel.Code,
                request.UpdateModel.ProcessId,
                request.UpdateModel.LongwallParametersId,
                request.UpdateModel.CuttingThicknessId,
                resolvedSeamFaceId.Value,
                request.UpdateModel.PowerId,
                request.UpdateModel.HardnessId,
                request.UpdateModel.PowerId.HasValue,
                request.UpdateModel.TechnologyId,
                request.UpdateModel.StartMonth,
                request.UpdateModel.EndMonth,
                request.UpdateModel.OtherMaterialValue,
                request.UpdateModel.Costs.Adapt<List<MaterialUnitPriceAssignmentCode>>()
                );

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(ProductUnitPriceCacheSignalKey);
            cacheService.InvalidateGroup(LongwallMaterialUnitPriceCacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
    }

}
