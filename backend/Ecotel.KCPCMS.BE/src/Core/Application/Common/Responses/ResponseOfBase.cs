#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Application.Common.Responses;

public abstract class ResponseOfBase
{
    public bool Success { get; set; }
    public string Message { get; set; }
}