using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Commands;
public record DeletePassportCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeletePassportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePassportCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Passport> _passportRepository = unitOfWork.GetRepository<Domain.Entities.Index.Passport>();
    public async Task<bool> Handle(DeletePassportCommand request, CancellationToken cancellationToken)
    {
        var existPassport = await _passportRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _passportRepository.Delete(existPassport);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}

