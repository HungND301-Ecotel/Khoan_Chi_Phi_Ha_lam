using System.Collections.ObjectModel;
using System.Net;

namespace Domain.Exceptions
{
    public class UnauthorizedException : CoreException
    {
        public UnauthorizedException()
            : base("authentication failed", new Collection<string>(), HttpStatusCode.Unauthorized)
        {
        }

        public UnauthorizedException(string message)
            : base(message, new Collection<string>(), HttpStatusCode.Unauthorized)
        {
        }
    }
}