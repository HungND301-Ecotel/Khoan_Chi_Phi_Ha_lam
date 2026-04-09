using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Catalog;

public class CodeService(IUnitOfWork unitOfWork) : ICodeService
{
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<AdjustmentFactor>();

    public async Task<bool> IsAdjustmentFactorCodeExisted(string code, DefaultIdType processGroupId)
    {
        return await _adjustmentFactorRepository.GetAll()
                .Where(p => p.Code.Value == code.ToUpper()
                            && p.ProcessGroupId == processGroupId)
                .AnyAsync();
    }

    public async Task<bool> IsAdjustmentFactorCodeExisted(string code, DefaultIdType processGroupId, DefaultIdType curId)
    {
        return await _adjustmentFactorRepository.GetAll()
                .Where(p => p.Code.Value == code.ToUpper()
                            && p.CodeId != curId
                            && p.ProcessGroupId == processGroupId)
                .AnyAsync();
    }

    public async Task<bool> IsCodeExisted(string code)
    {
        return await _codeRepository.AnyAsync(c => c.Value == code.ToUpper());
    }

    public async Task<bool> IsCodeExisted(string code, Guid curId)
    {
        return await _codeRepository.AnyAsync(c => c.Value == code.ToUpper() && c.Id != curId);
    }

    public async Task<bool> IsEquipmentCodeExisted(string code, Guid processGroupId)
    {
        var normalizedCode = code.ToUpper();
        return await _equipmentRepository.GetAll()
            .Where(e => e.Code != null
                        && e.Code.Value == normalizedCode
                        && e.EquipmentProcessGroups.Any(epg => epg.ProcessGroupId == processGroupId))
            .AnyAsync();
    }

    public async Task<bool> IsEquipmentCodeExisted(string code, Guid processGroupId, Guid curEquipmentId)
    {
        var normalizedCode = code.ToUpper();
        return await _equipmentRepository.GetAll()
            .Where(e => e.Id != curEquipmentId
                        && e.Code != null
                        && e.Code.Value == normalizedCode
                        && e.EquipmentProcessGroups.Any(epg => epg.ProcessGroupId == processGroupId))
            .AnyAsync();
    }

    public async Task<bool> IsPartCodeExisted(string code)
    {
        return await _codeRepository.AnyAsync(c => c.Value == code.ToUpper() && c.Part != null);
    }

    public async Task<bool> IsPartCodeExisted(string code, DefaultIdType curId)
    {
        return await _codeRepository.AnyAsync(c => c.Value == code.ToUpper() && c.Id != curId && c.Part != null);
    }

    public async Task<bool> IsProductCodeExisted(string code, DefaultIdType processGroupId)
    {
        return await _productRepository.GetAll()
            .Where(p => p.Code.Value == code.ToUpper()
                        && p.ProcessGroupId == processGroupId)
            .AnyAsync();
    }

    public async Task<bool> IsProductCodeExisted(string code, DefaultIdType processGroupId, DefaultIdType curId)
    {
        return await _productRepository.GetAll()
            .Where(p => p.Code.Value == code.ToUpper()
                        && p.CodeId != curId
                        && p.ProcessGroupId == processGroupId)
            .AnyAsync();
    }
}
