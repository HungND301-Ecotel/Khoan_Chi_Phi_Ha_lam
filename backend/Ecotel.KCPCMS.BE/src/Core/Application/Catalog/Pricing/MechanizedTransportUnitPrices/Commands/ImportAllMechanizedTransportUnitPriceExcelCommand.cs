using Application.Common.Exceptions;
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.Commands;

public record ImportAllMechanizedTransportUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAllMechanizedTransportUnitPriceExcelCommandHandler(IMediator mediator) : IRequestHandler<ImportAllMechanizedTransportUnitPriceExcelCommand, bool>
{
    private static readonly (string SheetName, string Label)[] SheetMap =
    {
        ("Xe Scania", "Xe Scania"),
        ("Xe hut bun chat thai", "Xe hút bùn chất thải"),
        ("Xe phuc vu va xe cau", "Xe phục vụ và xe cẩu"),
        ("May xuc va may gat", "Máy xúc và máy gạt")
    };

    public async Task<bool> Handle(ImportAllMechanizedTransportUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        byte[] sourceBytes;
        using (var memoryStream = new MemoryStream())
        {
            await request.File.CopyToAsync(memoryStream, cancellationToken);
            sourceBytes = memoryStream.ToArray();
        }

        var errors = new List<string>();

        foreach (var (sheetName, label) in SheetMap)
        {
            var extractedFile = ExtractSheetAsFormFile(sourceBytes, sheetName, $"{sheetName}.xlsx");
            if (extractedFile == null)
            {
                continue;
            }

            try
            {
                switch (sheetName)
                {
                    case "Xe Scania":
                        await mediator.Send(new Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Commands.ImportScaniaTruckUnitPriceExcelCommand(extractedFile), cancellationToken);
                        break;
                    case "Xe hut bun chat thai":
                        await mediator.Send(new Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Commands.ImportWasteSuctionTruckUnitPriceExcelCommand(extractedFile), cancellationToken);
                        break;
                    case "Xe phuc vu va xe cau":
                        await mediator.Send(new Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Commands.ImportServiceAndCraneVehicleUnitPriceExcelCommand(extractedFile), cancellationToken);
                        break;
                    case "May xuc va may gat":
                        await mediator.Send(new Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Commands.ImportExcavatorAndBulldozerUnitPriceExcelCommand(extractedFile), cancellationToken);
                        break;
                }
            }
            catch (ExcelImportException ex)
            {
                errors.AddRange(ex.ErrorMessages.Select(e => $"[{label}] {e}"));
            }
        }

        if (errors.Count > 0)
        {
            throw new ExcelImportException(errors);
        }

        return true;
    }

    private static IFormFile? ExtractSheetAsFormFile(byte[] sourceBytes, string sheetName, string fileName)
    {
        using var sourceStream = new MemoryStream(sourceBytes);
        using var sourceWorkbook = new XLWorkbook(sourceStream);
        var sourceSheet = sourceWorkbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);
        if (sourceSheet == null)
        {
            return null;
        }

        using var targetWorkbook = new XLWorkbook();
        sourceSheet.CopyTo(targetWorkbook, sourceSheet.Name);

        var outputStream = new MemoryStream();
        targetWorkbook.SaveAs(outputStream);
        outputStream.Position = 0;

        return new FormFile(outputStream, 0, outputStream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}