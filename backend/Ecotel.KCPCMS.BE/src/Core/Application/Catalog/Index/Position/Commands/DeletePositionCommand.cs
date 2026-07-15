using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Position.Commands;

public record DeletePositionCommand(int Id) : IRequest<bool>;

public class DeletePositionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePositionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository = unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<bool> Handle(DeletePositionCommand request, CancellationToken cancellationToken)
    {
        var existPosition = await _positionRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (existPosition.Employees.Any())
        {
            throw new ConflictException("Không thể xóa chức vụ đang được sử dụng.");
        }

        _positionRepository.Delete(existPosition);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}