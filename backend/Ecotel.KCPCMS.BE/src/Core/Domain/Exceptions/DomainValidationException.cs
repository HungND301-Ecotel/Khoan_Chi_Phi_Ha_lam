namespace Domain.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException()
    { }

    public DomainValidationException(string message)
        : base(message)
    { }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    { }
}