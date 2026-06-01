using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Product;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Product.Queries;

public record ExportExcelProductQuery() : IRequest<byte[]>;

public class ExportExcelProductQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelProductQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.Product> _productRepository = unitOfWork.GetRepository<Domain.Entities.Index.Product>();
    private readonly IWriteRepository<Domain.Entities.Index.ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProcessGroup>();
    public async Task<byte[]> Handle(ExportExcelProductQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(ProductExcelDto.Id));

        var list = await _productRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProcessGroup).ThenInclude(s => s.FixedKey)
                .Include(s => s.Code!),
            disableTracking: true);

        var processGroups = await _processGroupRepository.GetAllAsync(
            selector: u => u.FixedKey != null ? u.FixedKey.Key : string.Empty,
            include: u => u.Include(u => u.FixedKey),
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(ProductExcelDto.ProcessGroupCode), processGroups.ToList() }
        };

        var dtoList = list.Select(s => new ProductExcelDto
        {
            Id = s.Id,
            StartMonth = s.StartMonth,
            EndMonth = s.EndMonth,
            Code = s.Code?.Value ?? "",
            Name = s.Name,
            ProcessGroupCode = s.ProcessGroup?.FixedKey?.Key ?? ""
        });

        return excelService.ExportToExcel(dtoList, "Công đoạn sản xuất", listHiddenProperty, dropdownConfigs);
    }
}
