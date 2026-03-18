namespace Application.Common.Exceptions;

public class InternalServerException(string message, List<string>? errors = default) : CustomException(message, errors);