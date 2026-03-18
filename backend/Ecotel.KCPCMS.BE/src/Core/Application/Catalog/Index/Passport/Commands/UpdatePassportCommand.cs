using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Passport;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Commands;
public record UpdatePassportCommand(PassportDto UpdateModel) : IRequest<bool>;

public class UpdatePassportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdatePassportCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Passport> _passportRepository = unitOfWork.GetRepository<Domain.Entities.Index.Passport>();
    public async Task<bool> Handle(UpdatePassportCommand request, CancellationToken cancellationToken)
    {
        var existProcessGroup = await _passportRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existProcessGroup.Update(request.UpdateModel.Name, request.UpdateModel.Sd, request.UpdateModel.Sc);

        _passportRepository.Update(existProcessGroup);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
