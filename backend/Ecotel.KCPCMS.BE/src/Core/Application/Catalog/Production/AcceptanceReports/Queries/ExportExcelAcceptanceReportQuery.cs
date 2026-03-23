using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record DownloadAcceptanceReportExcelQuery(Guid AcceptanceReportId) : IRequest<byte[]>;

public class DownloadAcceptanceReportExcelQueryHandler(IUnitOfWork unitOfWork, IExcelService excelService) : IRequestHandler<DownloadAcceptanceReportExcelQuery, byte[]>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<byte[]> Handle(DownloadAcceptanceReportExcelQuery request, CancellationToken cancellationToken)
    {
        // Get AcceptanceReport with items
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == request.AcceptanceReportId,
            include: q => q
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                    .ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(m => m.Part)
                    .ThenInclude(p => p.Code),
            disableTracking: true);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Check if there are items in database
        if (acceptanceReport.AcceptanceReportItems != null && acceptanceReport.AcceptanceReportItems.Any())
        {
            return CreateExcelFromData(acceptanceReport.AcceptanceReportItems);
        }

        // If no items, create empty template
        return CreateTemplateExcel();
    }

    private byte[] CreateExcelFromData(IEnumerable<AcceptanceReportItem> items)
    {
        var hiddenProperties = new List<string> { nameof(AcceptanceReportExcelTemplateDto.Id) };

        // Map items to DTO
        var excelData = items.Select(item => new AcceptanceReportExcelTemplateDto
        {
            Id = item.Id,
            MaterialCode = item.Material?.Code?.Value ?? item?.Part?.Code?.Value ?? "",
            IssuedQuantity = item.IssuedQuantity,
            ShippedQuantity = item.ShippedQuantity
        }).ToList();

        return excelService.ExportToExcel(
            excelData,
            "Báo cáo tiếp nhận",
            hiddenProperties,
            null);
    }

    private byte[] CreateTemplateExcel()
    {
        var hiddenProperties = new List<string> { nameof(AcceptanceReportExcelTemplateDto.Id) };

        // Create empty template with headers only
        var templateData = new List<AcceptanceReportExcelTemplateDto>();

        return excelService.ExportToExcel(
            templateData,
            "Báo cáo tiếp nhận",
            hiddenProperties,
            null);
    }
}

