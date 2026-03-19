namespace Application.Interfaces.Services;

public interface ICodeService
{
    public Task<bool> IsCodeExisted(string code);
    public Task<bool> IsCodeExisted(string code, Guid curId);

    public Task<bool> IsProductCodeExisted(string code, Guid processGroupId);
    public Task<bool> IsProductCodeExisted(string code, Guid processGroupId, Guid curId);

    public Task<bool> IsAdjustmentFactorCodeExisted(string code, Guid processGroupId);
    public Task<bool> IsAdjustmentFactorCodeExisted(string code, Guid processGroupId, Guid curId);

    public Task<bool> IsPartCodeExisted(string code);
    public Task<bool> IsPartCodeExisted(string code, Guid curId);
}
