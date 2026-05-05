using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Product;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Product.Queries;
public record GetProductByIdQuery(DefaultIdType Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository = unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productExisted = await _productRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(t => t.ProcessGroup).ThenInclude(t => t.FixedKey)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ProductDto
        {
            Id = productExisted.Id,
            Code = productExisted.Code.Value,
            Name = productExisted.Name,
            ProcessGroupId = productExisted.ProcessGroupId,
            ProcessGroupCode = productExisted.ProcessGroup?.FixedKey?.Key ?? "",
            ProcessGroupName = productExisted.ProcessGroup?.Name ?? ""
        };
    }
}
