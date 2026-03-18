using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Product;
using Application.Interfaces.Services;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Product.Commands;

public record CreateProductCommand(CreateProductDto CreateModel) : IRequest<bool>;

public class CreateProductCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService)
    : IRequestHandler<CreateProductCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();

    public async Task<bool> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsProductCodeExisted(request.CreateModel.Code, request.CreateModel.ProcessGroupId))
        {
            throw new ConflictException(CustomResponseMessage.ProductCodeAlreadyExists);
        }

        var processGroup = await _processGroupRepository.GetFirstOrDefaultAsync(predicate: pg => pg.Id == request.CreateModel.ProcessGroupId, disableTracking: true) ??
                           throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);

        var newProduct = Domain.Entities.Index.Product.Create(request.CreateModel.Code, request.CreateModel.Name,
            request.CreateModel.ProcessGroupId);
        await _productRepository.InsertAsync(newProduct);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
