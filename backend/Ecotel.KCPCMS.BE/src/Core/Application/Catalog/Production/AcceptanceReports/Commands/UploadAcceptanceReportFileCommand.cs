using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
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
    private readonly IWriteRepository<MaintainUnitPriceEquipment> maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<MaintainUnitPriceEquipment>();

    public async Task<UploadAcceptanceReportResponseDto> Handle(UploadAcceptanceReportFileCommand request, CancellationToken cancellationToken)
    {
        var materials = await materialRepository.GetAllAsync(
            include: p => p.Include(p => p.UnitOfMeasure).Include(p => p.Code),
            disableTracking: true);

        var maintainUnitPriceEquipments = await maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: p => p.Include(p => p.Part).ThenInclude(p => p.Code)
                           .Include(p => p.Part).ThenInclude(p => p.UnitOfMeasure)
                           .Include(p => p.MaintainUnitPrice),
            disableTracking: true);

        if ((materials == null || !materials.Any()) && (maintainUnitPriceEquipments == null || !maintainUnitPriceEquipments.Any()))
        {
            throw new ApplicationException(CustomResponseMessage.MaterialPartNotFound);
        }

        using var fileStream = request.File.OpenReadStream();
        return await excelService.ProcessExcelFileAsync(
            request.ProductionOutputId,
            fileStream,
            request.File.FileName,
            materials ?? new List<Material>(),
            maintainUnitPriceEquipments ?? new List<MaintainUnitPriceEquipment>());
    }
}
