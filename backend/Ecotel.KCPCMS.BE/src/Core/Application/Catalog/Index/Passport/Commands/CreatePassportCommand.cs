using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Passport;
using MediatR;

namespace Application.Catalog.Index.Passport.Commands;
public record CreatePassportCommand(CreatePassportDto CreateModel) : IRequest<bool>;

public class CreatePassportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreatePassportCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Passport> _passportRepository = unitOfWork.GetRepository<Domain.Entities.Index.Passport>();
    public async Task<bool> Handle(CreatePassportCommand request, CancellationToken cancellationToken)
    {
        var newPassport = Domain.Entities.Index.Passport.Create(request.CreateModel.Name, request.CreateModel.Sd, request.CreateModel.Sc);
        await _passportRepository.InsertAsync(newPassport);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
