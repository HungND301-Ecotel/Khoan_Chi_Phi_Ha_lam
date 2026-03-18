using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

public class CurrentUserMiddleware(ICurrentUserInitializer currentUserInitializer) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        currentUserInitializer.SetCurrentUser(context.User);

        await next(context);
    }
}