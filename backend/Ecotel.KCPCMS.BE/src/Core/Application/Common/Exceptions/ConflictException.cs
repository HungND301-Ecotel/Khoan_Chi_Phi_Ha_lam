using System.Net;

namespace Application.Common.Exceptions;

public class ConflictException : CustomException
{
    public ConflictException()
        : base(string.Empty, null, HttpStatusCode.Conflict)
    {
    }

    public ConflictException(string message)
        : base(message, null, HttpStatusCode.Conflict)
    {
    }
}