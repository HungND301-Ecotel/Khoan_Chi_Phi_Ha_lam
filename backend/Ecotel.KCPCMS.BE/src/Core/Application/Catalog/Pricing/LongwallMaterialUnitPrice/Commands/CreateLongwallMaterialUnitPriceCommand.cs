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
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

    public async Task<bool> Handle(CreateLongwallMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.MaterialUnitPriceCodeAlreadyExists);
        }

        if (await _materialUnitPriceRepository.AnyAsync(m =>
            m.StartMonth < request.CreateModel.EndMonth &&
            m.EndMonth > request.CreateModel.StartMonth &&
            m.LongwallParametersId == request.CreateModel.LongwallParametersId &&
            m.CuttingThicknessId == request.CreateModel.CuttingThicknessId &&
            m.SeamFaceId == request.CreateModel.SeamFaceId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var assignemntCodeIds = request.CreateModel.Costs.Select(c => c.AssignmentCodeId).Distinct();
        var assignmentCodeTask = await _assignmentCodeRepository.GetAllAsync(selector: a => a.Id, disableTracking: true);

        var checkExisted = assignemntCodeIds.All(id => assignmentCodeTask.Any(ac => ac == id));

        if (!checkExisted)
        {
            throw new Exception("Một hoặc nhiều Mã giao khoán không tồn tại.");
        }

        bool longwallParamsTask = await _longwallParametersRepository.AnyAsync(p => p.Id == request.CreateModel.LongwallParametersId);
        bool cuttingThicknessTask = await _cuttingThicknessRepository.AnyAsync(p => p.Id == request.CreateModel.CuttingThicknessId);
        bool seamFaceTask = await _seamFaceRepository.AnyAsync(p => p.Id == request.CreateModel.SeamFaceId);
        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProcessId);

        bool technologyTask = true;
        if (request.CreateModel.TechnologyId.HasValue)
        {
            technologyTask = await _technologyRepository.AnyAsync(p => p.Id == request.CreateModel.TechnologyId.Value);
        }

        var checkData = longwallParamsTask && cuttingThicknessTask && seamFaceTask && processTask && technologyTask;
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
                request.CreateModel.SeamFaceId,
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
