using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using MediatR;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record CreateSavingsRateConfigCommand(CreateSavingsRateConfigDto CreateModel) : IRequest<bool>;

public class CreateSavingsRateConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateSavingsRateConfigCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(CreateSavingsRateConfigCommand request, CancellationToken cancellationToken)
    {
        var newSavingsRateConfig = SavingsRateConfigEntity.Create(
            request.CreateModel.MaxRevenue,
            request.CreateModel.MaxSavingsRate,
            request.CreateModel.Description);

        await _savingsRateConfigRepository.InsertAsync(newSavingsRateConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
