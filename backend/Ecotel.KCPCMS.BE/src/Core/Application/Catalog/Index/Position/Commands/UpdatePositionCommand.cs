using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Position;
using Application.Interfaces.Services;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Position.Commands;

public record UpdatePositionCommand(UpdatePositionDto Dto) : IRequest<bool>;

public class UpdatePositionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdatePositionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<bool> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _positionRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Dto.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (string.IsNullOrWhiteSpace(request.Dto.Code))
        {
            throw new BadRequestException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        var code = request.Dto.Code.Trim().ToUpper();
        if (await _positionRepository.ExistsAsync(p => p.Code == code && p.Id != request.Dto.Id))
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        entity.Update(request.Dto.Name.Trim(), request.Dto.Level ?? 0, request.Dto.Description, code);
        _positionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
