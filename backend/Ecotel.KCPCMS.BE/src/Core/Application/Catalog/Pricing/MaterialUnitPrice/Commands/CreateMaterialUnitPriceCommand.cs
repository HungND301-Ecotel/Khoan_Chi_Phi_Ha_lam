using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Commands;

public record CreateMaterialUnitPriceCommand(CreateMaterialUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateMaterialUnitPriceCommandHandler(
    IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateMaterialUnitPriceCommand, bool>
{
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    private readonly IWriteRepository<SupportStep> _supportStepRepository = unitOfWork.GetRepository<SupportStep>();
    public async Task<bool> Handle(CreateMaterialUnitPriceCommand request, CancellationToken cancellationToken)
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
            m.InsertItemId == request.CreateModel.InsertItemId &&
            m.SupportStepId == request.CreateModel.SupportStepId))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        bool processTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProcessId);
        bool passportTask = await _passportRepository.AnyAsync(p => p.Id == request.CreateModel.PassportId);
        bool hardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId);
        bool insertItemTask = await _insertItemRepository.AnyAsync(p => p.Id == request.CreateModel.InsertItemId);
        bool supportStepTask = await _supportStepRepository.AnyAsync(p => p.Id == request.CreateModel.SupportStepId);

        var checkData = processTask && passportTask && hardnessTask && insertItemTask && supportStepTask;
        if (!checkData)
        {
            throw new BadRequestException(CustomResponseMessage.OneOrMoreReferencedSpecificationIdsInvalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var newMaterialUnitPrice = TunnelExcavationMaterialUnitPrice.Create(
                request.CreateModel.Code,
                request.CreateModel.ProcessId,
                request.CreateModel.PassportId,
                request.CreateModel.HardnessId,
                request.CreateModel.InsertItemId,
                request.CreateModel.SupportStepId,
                null,
                request.CreateModel.StartMonth,
                request.CreateModel.EndMonth,
                request.CreateModel.TotalPrice);

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
