using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Product;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Product.Commands;

public record UpdateProductCommand(UpdateProductDto UpdateModel) : IRequest<bool>;

public class UpdateProductCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository = unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existProduct = await _productRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: t => t.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsProductCodeExisted(request.UpdateModel.Code, request.UpdateModel.ProcessGroupId, existProduct.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.ProductCodeAlreadyExists);
        }

        var processGroup = await _processGroupRepository.GetFirstOrDefaultAsync(predicate: pg => pg.Id == request.UpdateModel.ProcessGroupId, disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);

        existProduct.Update(
            code: request.UpdateModel.Code,
            name: request.UpdateModel.Name,
            processGroupId: request.UpdateModel.ProcessGroupId,
            startMonth: request.UpdateModel.StartMonth,
            endMonth: request.UpdateModel.EndMonth);

        _productRepository.Update(existProduct);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
