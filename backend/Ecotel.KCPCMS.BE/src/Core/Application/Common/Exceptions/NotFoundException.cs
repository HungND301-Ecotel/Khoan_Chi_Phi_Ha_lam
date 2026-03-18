using System.Net;

namespace Application.Common.Exceptions;

public class NotFoundException(string message) : CustomException(message, null, HttpStatusCode.NotFound);