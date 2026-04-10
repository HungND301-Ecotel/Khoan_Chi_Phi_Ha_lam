using Application.Common.Interfaces;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Entities.Index;
using Domain.Entities.Pricing;

namespace Application.Interfaces.Services;

public interface IAcceptanceReportExcelService : ITransientService
{
    Task<UploadAcceptanceReportResponseDto> ProcessExcelFileAsync(
        Guid Id,
        Stream fileStream,
        string fileName,
        IEnumerable<Material> materialsInDb,
        IEnumerable<MaintainUnitPriceEquipment> maintainUnitPriceEquipmentsInDb);

}

