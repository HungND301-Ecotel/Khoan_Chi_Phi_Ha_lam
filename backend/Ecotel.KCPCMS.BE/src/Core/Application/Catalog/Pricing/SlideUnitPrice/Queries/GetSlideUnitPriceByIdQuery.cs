using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SlideUnitPrice;
using Application.Dto.Catalog.SlideUnitPriceAssignmentCode;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;

public record GetSlideUnitPriceByIdQuery(DefaultIdType Id) : IRequest<SlideUnitPriceDetailDto>;

public class GetSlideUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSlideUnitPriceByIdQuery, SlideUnitPriceDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    public async Task<SlideUnitPriceDetailDto> Handle(GetSlideUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var slideUnitPrice = await _slideUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(u => u.SlideUnitPriceAssignmentCodes).ThenInclude(u => u.Material).ThenInclude(u => u.AssignmentCode).ThenInclude(a => a.Code)
                .Include(u => u.SlideUnitPriceAssignmentCodes).ThenInclude(u => u.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(u => u.SlideUnitPriceAssignmentCodes).ThenInclude(u => u.Material).ThenInclude(m => m.Costs)
                .Include(u => u.SlideUnitPriceAssignmentCodes).ThenInclude(u => u.Material).ThenInclude(m => m.Code)
                .Include(t => t.ProcessGroup).Include(t => t.Passport).Include(t => t.Hardness)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var materialCosts = slideUnitPrice.SlideUnitPriceAssignmentCodes
            .GroupBy(a => a.Material?.AssigmentCodeId)
            .Select(group =>
            {
                var costs = group.Select(c =>
                {
                    var curCost = c.Material?.Costs.FirstOrDefault(c =>
                        c.StartMonth <= slideUnitPrice.StartMonth && c.EndMonth >= slideUnitPrice.StartMonth)?.Amount ?? 0;

                    return new MaterialCostDto
                    {
                        Id = c.Id,
                        MaterialId = c.MaterialId,
                        MaterialCode = c.Material?.Code?.Value ?? "",
                        MaterialName = c.Material?.Name ?? "",
                        UnitOfMeasureName = c.Material?.UnitOfMeasure?.Name ?? "",
                        Cost = curCost,
                        Amount = c.Amount
                    };
                }).ToList();

                return new SlideUnitPriceAssignmentCodeDetailDto
                {
                    AssignmentCodeId = group.Key ?? Guid.Empty,
                    AssignmentCode = group.First().Material?.AssignmentCode?.Code?.Value ?? "",
                    AssignmentCodeName = group.First().Material?.AssignmentCode?.Name ?? "",
                    Costs = costs
                };
            }).ToList();
        
        string passportName =
            $"H/c {slideUnitPrice.Passport!.Name}; {slideUnitPrice.Passport!.Sd}; {slideUnitPrice.Passport!.Sc}";
        return new SlideUnitPriceDetailDto
        {
            Id = slideUnitPrice.Id,
            Code = slideUnitPrice.Code.Value,
            Name = $"{slideUnitPrice.ProcessGroup?.Name}, {passportName}, {slideUnitPrice.Hardness?.Value}",
            StartMonth = slideUnitPrice.StartMonth,
            EndMonth = slideUnitPrice.EndMonth,
            MaterialCost = materialCosts
        };
    }
}
