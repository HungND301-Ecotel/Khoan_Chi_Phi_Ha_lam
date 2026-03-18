namespace Application.Common.Models;

public interface IOrderBy : ISpecPayload
{
    string[]? OrderBy { get; set; }

    bool HasOrderBy();

    bool ForceUseCustomOrderBy { get; set; }
}