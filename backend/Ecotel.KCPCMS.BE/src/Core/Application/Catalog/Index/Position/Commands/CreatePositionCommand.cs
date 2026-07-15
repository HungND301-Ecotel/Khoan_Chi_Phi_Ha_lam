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

public record CreatePositionCommand(CreatePositionDto Dto) : IRequest<bool>;

public class CreatePositionCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreatePositionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<bool> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
 
        if (await codeService.IsCodeExisted(request.Dto.Name))
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        if (await codeService.IsCodeExisted(request.Dto.Description))
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        var entity = Domain.Entities.Index.Position.Create(request.Dto.Name.Trim(),request.Dto.Level ?? 0,request.Dto.Description.Trim());
        await _positionRepository.InsertAsync(entity);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
