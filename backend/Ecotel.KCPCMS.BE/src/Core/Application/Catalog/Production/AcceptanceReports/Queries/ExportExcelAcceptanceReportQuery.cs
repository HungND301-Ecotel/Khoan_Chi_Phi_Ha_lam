using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using System.Globalization;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record DownloadAcceptanceReportExcelQuery(Guid AcceptanceReportId) : IRequest<byte[]>;

public class DownloadAcceptanceReportExcelQueryHandler(IUnitOfWork unitOfWork, IExcelService excelService) : IRequestHandler<DownloadAcceptanceReportExcelQuery, byte[]>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<byte[]> Handle(DownloadAcceptanceReportExcelQuery request, CancellationToken cancellationToken)
    {
        // Get AcceptanceReport with items and ProductionOutput
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == request.AcceptanceReportId,
            include: q => q
                .Include(a => a.ProductionOutput)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                    .ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Part)
                    .ThenInclude(p => p.Code)
                 .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(a => a.IssuedDetails)
                 .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(a => a.ShippedDetails),
            disableTracking: true);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        return CreateMultiSheetExcel(acceptanceReport);
    }

    private byte[] CreateMultiSheetExcel(AcceptanceReport acceptanceReport)
    {
        var hiddenProperties = new List<string> { nameof(AcceptanceReportExcelTemplateDto.Id) };
        var startMonth = acceptanceReport.ProductionOutput?.StartMonth;
        int daysInMonth = startMonth.HasValue
            ? DateTime.DaysInMonth(startMonth.Value.Year, startMonth.Value.Month)
            : 31;

        var items = acceptanceReport.AcceptanceReportItems ?? Enumerable.Empty<AcceptanceReportItem>();
        var itemsByDay = items
            .GroupBy(i => i.PostingDate?.Day ?? 1)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sheets = new Dictionary<string, IEnumerable<AcceptanceReportExcelTemplateDto>>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var sheetName = day.ToString("D2");
            if (itemsByDay.TryGetValue(day, out var dayItems) && dayItems.Any())
            {
                var excelData = dayItems.Select(item => new AcceptanceReportExcelTemplateDto
                {
                    Id = item.Id,
                    DocumentNumber = item.DocumentNumber,
                    PostingDate = item.PostingDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    MaterialCode = ResolveTrackedMaterialCode(item),
                    IssuedQuantity = item.IssuedQuantity,
                    ShippedQuantity = item.ShippedQuantity
                }).ToList();

                sheets[sheetName] = excelData;
            }
            else
            {
                sheets[sheetName] = new List<AcceptanceReportExcelTemplateDto>();
            }
        }

        return excelService.ExportMultiSheet(sheets, hiddenProperties);
    }

    private static string ResolveTrackedMaterialCode(AcceptanceReportItem item)
        => item.IsTrackedSctxItem
            ? item.Part?.Code?.Value ?? item.Material?.Code?.Value ?? string.Empty
            : item.Material?.Code?.Value ?? item.Part?.Code?.Value ?? string.Empty;
}


