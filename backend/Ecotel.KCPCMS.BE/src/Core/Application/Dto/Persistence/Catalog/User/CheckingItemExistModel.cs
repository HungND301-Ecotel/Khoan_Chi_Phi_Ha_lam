namespace Application.Dto.Persistence.Catalog.User;

public class CheckingItemExistModel
{
    public CheckingItemExistModel(bool existed, bool activated, string value)
    {
        Existed = existed;
        IsActivated = activated;
        Value = value;
    }

    public CheckingItemExistModel(string value)
    {
        Value = value;
        Existed = false;
        IsActivated = null;
    }

    public bool Existed { get; set; }
    public bool HasPassword { get; set; }
    public bool? IsActivated { get; set; }
    public bool AttachedOrganization { get; set; }
    public string Value { get; set; }
}