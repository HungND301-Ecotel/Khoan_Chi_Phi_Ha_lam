using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Commands;

public record CreateAssignmentCodeCommand(CreateAssignmentCodeDto CreateModel) : IRequest<bool>;

public class CreateAssignmentCodeCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateAssignmentCodeCommand, bool>
{
    private readonly IWriteRepository<AssignmentCode> _assignemntcodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<AssignmentCodeMaterial> _assignmentCodeMaterialRepository = unitOfWork.GetRepository<AssignmentCodeMaterial>();
    public async Task<bool> Handle(CreateAssignmentCodeCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.AssignmentCodeAlreadyExists);
        }

        var materialIds = request.CreateModel.MaterialIds.Distinct().ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var newAssignmentCode = AssignmentCode.Create(request.CreateModel.Name, request.CreateModel.Code, request.CreateModel.UnitOfMeasureId);
            await _assignemntcodeRepository.InsertAsync(newAssignmentCode, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            if (materialIds.Any())
            {
                var materials = await _materialRepository.GetAllAsync(
                    predicate: m => materialIds.Contains(m.Id),
                    include: m => m.Include(x => x.Code),
                    disableTracking: false);

                if (materials.Count != materialIds.Count)
                {
                    throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
                }

                var links = materials
                    .Select(material => AssignmentCodeMaterial.Create(newAssignmentCode, material))
                    .ToList();

                await _assignmentCodeMaterialRepository.InsertAsync(links, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}
