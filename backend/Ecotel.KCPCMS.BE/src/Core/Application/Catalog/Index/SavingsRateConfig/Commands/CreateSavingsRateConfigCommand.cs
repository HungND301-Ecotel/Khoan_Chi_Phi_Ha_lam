using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using MediatR;
using System.Globalization;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record CreateSavingsRateConfigCommand(CreateSavingsRateConfigDto CreateModel) : IRequest<bool>;

public class CreateSavingsRateConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateSavingsRateConfigCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(CreateSavingsRateConfigCommand request, CancellationToken cancellationToken)
    {
        var revenueDisplay = request.CreateModel.RevenueDisplay
            ?? (request.CreateModel.MaxRevenue.HasValue
                ? $"≤ {request.CreateModel.MaxRevenue.Value.ToString(CultureInfo.InvariantCulture)}"
                : null);

        var savingsRateDisplay = request.CreateModel.SavingsRateDisplay
            ?? (request.CreateModel.MaxSavingsRate.HasValue
                ? $"≤ {request.CreateModel.MaxSavingsRate.Value.ToString(CultureInfo.InvariantCulture)}%"
                : null);

        var newSavingsRateConfig = SavingsRateConfigEntity.Create(
            revenueDisplay,
            savingsRateDisplay,
            request.CreateModel.Description);

        await _savingsRateConfigRepository.InsertAsync(newSavingsRateConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
