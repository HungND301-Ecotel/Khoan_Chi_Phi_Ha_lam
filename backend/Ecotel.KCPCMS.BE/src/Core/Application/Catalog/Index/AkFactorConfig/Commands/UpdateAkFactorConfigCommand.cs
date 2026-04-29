using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using Domain.Common.Enums;
using MediatR;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;
using ProcessGroup = Domain.Entities.Index.ProcessGroup;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record UpdateAkFactorConfigCommand(AkFactorConfigDto UpdateModel) : IRequest<bool>;

public class UpdateAkFactorConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAkFactorConfigCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(UpdateAkFactorConfigCommand request, CancellationToken cancellationToken)
    {
        var existAkFactorConfig = await _akFactorConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var processGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.ProcessGroupId,
            disableTracking: true);
        if (processGroup == null)
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        ValidateAkFactorConfig(processGroup.Type, request.UpdateModel.AkDiffDisplay, request.UpdateModel.AdjustmentRateDisplay);

        existAkFactorConfig.Update(
            request.UpdateModel.ProcessGroupId,
            request.UpdateModel.AkDiffDisplay,
            request.UpdateModel.AdjustmentRateDisplay,
            request.UpdateModel.Description);

        _akFactorConfigRepository.Update(existAkFactorConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }

    private static void ValidateAkFactorConfig(ProcessGroupType processGroupType, string? akDiffDisplay, string? adjustmentRateDisplay)
    {
        if (!AkFactorConfigEntity.SupportsProcessGroupType(processGroupType))
        {
            throw new BadRequestException("Nhóm công đoạn sản xuất không hợp lệ để cấu hình hệ số Ak.");
        }

        if (!AkFactorConfigEntity.HasValidAkDiffCondition(akDiffDisplay))
        {
            throw new BadRequestException("Chênh lệch Ak không đúng định dạng. Vui lòng dùng dạng > 0, <= -0,5 hoặc = 1.");
        }

        if (!AkFactorConfigEntity.HasValidAdjustmentRate(adjustmentRateDisplay))
        {
            throw new BadRequestException("Tỷ lệ điều chỉnh doanh thu không đúng định dạng. Vui lòng dùng một giá trị duy nhất, ví dụ 1,5%.");
        }
    }
}
