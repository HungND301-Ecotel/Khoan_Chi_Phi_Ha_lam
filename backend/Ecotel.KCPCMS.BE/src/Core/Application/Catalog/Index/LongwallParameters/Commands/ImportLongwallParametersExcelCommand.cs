using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallParameters;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using LongwallParametersEntity = Domain.Entities.Index.LongwallParameters;

namespace Application.Catalog.Index.LongwallParameters.Commands;

public record ImportLongwallParametersExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportLongwallParametersExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportLongwallParametersExcelCommand, bool>
{
    private readonly IWriteRepository<LongwallParametersEntity> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParametersEntity>();
    public async Task<bool> Handle(ImportLongwallParametersExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<LongwallParametersExcelDto>(stream);
        var excelDtos = dtos.Select(d => LongwallParametersEntity.Create(d.Id, d.Llc, d.Lkc, d.Mk));
        var dbLongwallParameters = await _longwallParametersRepository.GetAllAsync(
            disableTracking: true);

        var deleteList = new List<LongwallParametersEntity>();
        var updateList = new List<LongwallParametersEntity>();
        var addList = new List<LongwallParametersEntity>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbLongwallParameters.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbLongwallParameters.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbLongwallParameters.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Llc, dto.Lkc, dto.Mk);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(LongwallParametersEntity.Create(dto.Llc, dto.Lkc, dto.Mk));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _longwallParametersRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _longwallParametersRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _longwallParametersRepository.Update(updateList);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }
}
