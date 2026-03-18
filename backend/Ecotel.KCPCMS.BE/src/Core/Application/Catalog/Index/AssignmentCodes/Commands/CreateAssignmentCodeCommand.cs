using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Commands;

public record CreateAssignmentCodeCommand(CreateAssignmentCodeDto CreateModel) : IRequest<bool>;

public class CreateAssignmentCodeCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateAssignmentCodeCommand, bool>
{
    private readonly IWriteRepository<AssignmentCode> _assignemntcodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    public async Task<bool> Handle(CreateAssignmentCodeCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.AssignmentCodeAlreadyExists);
        }

        var newAssignmentCode = AssignmentCode.Create(request.CreateModel.Name, request.CreateModel.Code, request.CreateModel.UnitOfMeasureId);
        await _assignemntcodeRepository.InsertAsync(newAssignmentCode);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}