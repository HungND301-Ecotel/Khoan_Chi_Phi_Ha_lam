#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Application.Common.Responses;

[Serializable]
public class ResponseBase<TResult> : ResponseOfBase
{
    public TResult? Result { get; set; }
    public ResponseBase(TResult result)
    {
        Result = result;
        Success = true;
    }

    public ResponseBase(TResult result, string message)
    {
        Result = result;
        Success = true;
        Message = message;
    }
    public ResponseBase()
    {
        Success = true;
    }
    public ResponseBase(string message)
    {
        Message = message;
        Success = true;
    }
    public ResponseBase(bool success)
    {
        Success = success;
    }
}