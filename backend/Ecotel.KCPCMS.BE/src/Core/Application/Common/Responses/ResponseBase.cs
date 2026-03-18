#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Application.Common.Responses;

[Serializable]
public class ResponseBase : ResponseBase<object>
{
    public ResponseBase()
    {
    }

    public ResponseBase(bool success)
        : base(success)
    {
    }

    public ResponseBase(object result)
        : base(result)
    {
    }
}