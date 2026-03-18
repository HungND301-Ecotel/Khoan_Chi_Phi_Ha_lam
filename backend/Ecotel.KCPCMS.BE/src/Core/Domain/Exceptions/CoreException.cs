using System.Net;

namespace Domain.Exceptions
{
    public class CoreException : Exception
    {
        public IEnumerable<string> ErrorMessages { get; }

        public HttpStatusCode StatusCode { get; }

        public CoreException(string message, IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            ErrorMessages = errors;
            StatusCode = statusCode;
        }

        public CoreException(string message) : base(message)
        {
            ErrorMessages = new List<string>();
        }
    }
}