using System.Net;

namespace Application.Common.Exceptions;

public class BadRequestException(string message) : CustomException(message, null, HttpStatusCode.BadRequest);