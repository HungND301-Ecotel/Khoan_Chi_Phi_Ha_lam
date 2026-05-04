using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;

public record CreateLowValuePerishableSupplyUnitPriceCommand(IList<CreateLowValuePerishableSupplyUnitPriceDto> CreateModel) : IRequest<bool>;

public class CreateLowValuePerishableSupplyUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<CreateLowValuePerishableSupplyUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(CreateLowValuePerishableSupplyUnitPriceCommand request, CancellationToken cancellationToken)
    {
        if (request.CreateModel is null || !request.CreateModel.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        List<Guid> departmentIds = request.CreateModel.Select(x => x.DepartmentId).Distinct().ToList();
        List<Guid> processGroupIds = request.CreateModel.Select(x => x.ProcessGroupId).Distinct().ToList();

        var departments = await _departmentRepository.GetAllAsync(
            predicate: d => departmentIds.Contains(d.Id),
            disableTracking: true);
        if (departments.Count != departmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: pg => processGroupIds.Contains(pg.Id),
            include: pg => pg.Include(x => x.FixedKey),
            disableTracking: true);
        if (processGroups.Count != processGroupIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
        }

        Dictionary<Guid, ProcessGroup> processGroupDict = processGroups.ToDictionary(x => x.Id, x => x);

        foreach (CreateLowValuePerishableSupplyUnitPriceDto model in request.CreateModel)
        {
            ValidateProcessGroupType(model.Type, processGroupDict[model.ProcessGroupId]);

            bool overlapExists = await _repository.ExistsAsync(e =>
                e.DepartmentId == model.DepartmentId &&
                e.ProcessGroupId == model.ProcessGroupId &&
                e.Type == model.Type &&
                e.StartMonth <= model.EndMonth &&
                e.EndMonth >= model.StartMonth);

            if (overlapExists)
            {
                throw new ConflictException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceAlreadyExists);
            }
        }

        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> entities = request.CreateModel
            .Select(model => Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice.Create(
                model.DepartmentId,
                model.ProcessGroupId,
                model.StartMonth,
                model.EndMonth,
                model.Type,
                model.TotalPrice))
            .ToList();

        await _repository.InsertAsync(entities, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);
        cacheService.InvalidateGroup(ModuleCacheSignalKey);
        return true;
    }

    private static void ValidateProcessGroupType(LowValuePerishableSupplyType type, ProcessGroup processGroup)
    {
        ProcessGroupType expectedType = type switch
        {
            LowValuePerishableSupplyType.TunnelExcavation => ProcessGroupType.DL,
            LowValuePerishableSupplyType.Longwall => ProcessGroupType.LC,
            _ => ProcessGroupType.None,
        };

        var actualType = processGroup.FixedKey?.Type ?? ProcessGroupType.None;

        if (actualType != expectedType)
        {
            throw new ConflictException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }
}