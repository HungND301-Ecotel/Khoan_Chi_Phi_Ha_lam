using System.Net;

namespace Application.Common.Exceptions
{
    public class TokenExpiredException : CustomException
    {
        public TokenExpiredException()
            : base("Authentication token has expired", [], HttpStatusCode.Gone)
        {
        }

        public TokenExpiredException(string message)
            : base(message, [], HttpStatusCode.Gone)
        {
        }

        public TokenExpiredException(string message, Exception innerException)
            : base(message, [], HttpStatusCode.Gone)
        {
        }
    }
}