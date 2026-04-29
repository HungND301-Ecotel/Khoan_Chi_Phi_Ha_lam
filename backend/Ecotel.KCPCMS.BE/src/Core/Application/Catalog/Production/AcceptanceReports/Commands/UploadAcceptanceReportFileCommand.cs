using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

public record UploadAcceptanceReportFileCommand(IFormFile File, Guid ProductionOutputId) : IRequest<UploadAcceptanceReportResponseDto>;

public class UploadAcceptanceReportFileCommandHandler(
    IAcceptanceReportExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<UploadAcceptanceReportFileCommand, UploadAcceptanceReportResponseDto>
{
    private readonly IWriteRepository<Material> materialRepository = unitOfWork.GetRepository<Material>();
    private readonly IWriteRepository<Part> partRepository = unitOfWork.GetRepository<Part>();

    public async Task<UploadAcceptanceReportResponseDto> Handle(UploadAcceptanceReportFileCommand request, CancellationToken cancellationToken)
    {
        var materials = await materialRepository.GetAllAsync(
            include: p => p.Include(p => p.UnitOfMeasure).Include(p => p.Code),
            disableTracking: true);

        var parts = await partRepository.GetAllAsync(
            include: p => p.Include(p => p.Code)
                           .Include(p => p.UnitOfMeasure),
            disableTracking: true);

        using var fileStream = request.File.OpenReadStream();
        return await excelService.ProcessExcelFileAsync(
            request.ProductionOutputId,
            fileStream,
            request.File.FileName,
            materials ?? new List<Material>(),
            parts ?? new List<Part>());
    }
}
