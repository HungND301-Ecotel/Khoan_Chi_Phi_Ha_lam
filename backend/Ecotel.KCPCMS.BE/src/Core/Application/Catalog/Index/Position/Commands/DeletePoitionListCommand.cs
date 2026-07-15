using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;

namespace Application.Catalog.Index.Position.Commands;

public record DeletePositionListCommand(IList<int> Ids) : IRequest<bool>;

public class DeletePositionListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePositionListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository = unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<bool> Handle(DeletePositionListCommand request, CancellationToken cancellationToken)
    {
        var entities = await _positionRepository.GetAllAsync(
            predicate: p => request.Ids.Contains(p.Id),
            disableTracking: true);

        if (entities.Any(p => p.Employees.Any()))
        {
            throw new ConflictException("Một hoặc nhiều chức vụ đang được sử dụng, không thể xóa.");
        }

        _positionRepository.Delete(entities);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
