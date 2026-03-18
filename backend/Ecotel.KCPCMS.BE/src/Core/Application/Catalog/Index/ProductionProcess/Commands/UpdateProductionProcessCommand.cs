using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionProcess;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProductionProcess.Commands;
public record UpdateProductionProcessCommand(UpdateProductionProcessDto UpdateModel) : IRequest<bool>;

public class UpdateProductionProcessCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateProductionProcessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    public async Task<bool> Handle(UpdateProductionProcessCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existProductionProcess = await _productionProcessRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.UpdateModel.Id,
                include: t => t.Include(t => t.Code),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            if (await codeService.IsCodeExisted(request.UpdateModel.Code, existProductionProcess.CodeId))
            {
                throw new ConflictException(CustomResponseMessage.ProductionProcessCodeAlreadyExists);
            }

            existProductionProcess.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.ProcessGroupId);

            _productionProcessRepository.Update(existProductionProcess);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken); ;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
        return true;
    }
}
