using System.Net;

namespace Application.Common.Exceptions;

public class ForbiddenException(string message) : CustomException(message, null, HttpStatusCode.Forbidden);