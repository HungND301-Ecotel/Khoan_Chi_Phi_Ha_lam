using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using MediatR;
using System.Globalization;
using Shared.Constants;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record UpdateSavingsRateConfigCommand(SavingsRateConfigDto UpdateModel) : IRequest<bool>;

public class UpdateSavingsRateConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateSavingsRateConfigCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(UpdateSavingsRateConfigCommand request, CancellationToken cancellationToken)
    {
        var existSavingsRateConfig = await _savingsRateConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var revenueDisplay = request.UpdateModel.RevenueDisplay
            ?? (request.UpdateModel.MaxRevenue.HasValue
                ? $"≤ {request.UpdateModel.MaxRevenue.Value.ToString(CultureInfo.InvariantCulture)}"
                : null);

        var savingsRateDisplay = request.UpdateModel.SavingsRateDisplay
            ?? (request.UpdateModel.MaxSavingsRate.HasValue
                ? $"≤ {request.UpdateModel.MaxSavingsRate.Value.ToString(CultureInfo.InvariantCulture)}%"
                : null);

        existSavingsRateConfig.Update(
            revenueDisplay,
            savingsRateDisplay,
            request.UpdateModel.Description);

        _savingsRateConfigRepository.Update(existSavingsRateConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
