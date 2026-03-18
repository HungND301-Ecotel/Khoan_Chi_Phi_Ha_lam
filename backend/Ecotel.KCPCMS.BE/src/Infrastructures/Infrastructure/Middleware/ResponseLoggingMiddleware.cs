using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace Infrastructure.Middleware;

public class ResponseLoggingMiddleware(ICurrentUser currentUser) : IMiddleware
{
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        var originalBodyStream = httpContext.Response.Body;

        await using var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        await next(httpContext);

        responseBodyStream.Seek(0, SeekOrigin.Begin);

        string responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

        if (httpContext.Request.Path.ToString().Contains("tokens"))
        {
            responseBody = "[Redacted] Contains Sensitive Information.";
        }

        string email = currentUser.GetUserEmail() ?? "Anonymous";
        long userId = currentUser.GetUserId();
        if (userId != 0)
        {
            LogContext.PushProperty("UserId", userId);
        }

        LogContext.PushProperty("UserEmail", email);
        LogContext.PushProperty("StatusCode", httpContext.Response.StatusCode);
        LogContext.PushProperty("ResponseTimeUTC", DateTime.UtcNow);
        Log.ForContext(
                "ResponseHeaders",
                httpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                destructureObjects: true)
            .ForContext("ResponseBody", responseBody)
            .Information(
                "HTTP {RequestMethod} Request to {RequestPath} by {RequesterEmail} has Status Code {StatusCode}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                email,
                httpContext.Response.StatusCode);

        responseBodyStream.Seek(0, SeekOrigin.Begin);

        await responseBodyStream.CopyToAsync(originalBodyStream);

        httpContext.Response.Body = originalBodyStream;
    }
}