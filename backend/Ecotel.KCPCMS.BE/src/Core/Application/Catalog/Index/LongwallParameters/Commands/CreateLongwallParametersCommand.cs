using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallParameters;
using MediatR;

namespace Application.Catalog.Index.LongwallParameters.Commands;

public record CreateLongwallParametersCommand(CreateLongwallParametersDto CreateModel) : IRequest<bool>;

public class CreateLongwallParametersCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateLongwallParametersCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<Domain.Entities.Index.LongwallParameters>();
    public async Task<bool> Handle(CreateLongwallParametersCommand request, CancellationToken cancellationToken)
    {
        var newLongwallParameters = Domain.Entities.Index.LongwallParameters.Create(request.CreateModel.Llc, request.CreateModel.Lkc, request.CreateModel.Mk);
        await _longwallParametersRepository.InsertAsync(newLongwallParameters);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
