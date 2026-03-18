using System.Net;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using Shared.Domain;

namespace Infrastructure.Middleware;

public class ExceptionMiddleware(
        ICurrentUser currentUser,
        ISerializerService jsonSerializer)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            // common response model
            var customErrorInfo = GetCodeAndMessage(exception);
            var errorResult = new ErrorResult
            {
                Status = customErrorInfo.Status,
                Title = customErrorInfo.Message,
                Errors = GetErrors(exception)
            };

            // logging
            LogContext.PushProperty("UserId", currentUser.GetUserId() != 0 ? currentUser.GetUserId() : string.Empty);
            LogContext.PushProperty("UserEmail", currentUser.GetUserEmail() ?? "Anonymous");
            string errorId = Guid.NewGuid().ToString();
            LogContext.PushProperty("ErrorId", errorId);
            Log.Error(exception, $"Request failed with StatusCode = {customErrorInfo.Status}, Id = {errorId}");

            // change response behaviour
            var response = context.Response;
            if (!response.HasStarted)
            {
                response.ContentType = "application/json";
                response.StatusCode = (int)errorResult.Status;
                await response.WriteAsync(jsonSerializer.Serialize(errorResult));
            }
        }
    }

    public static (int Status, string Message) GetCodeAndMessage(Exception ex)
    {
        string message;
        int statusCode;
        switch (ex)
        {
            case ConflictException e:
                statusCode = (int)e.StatusCode;
                message = e.Message;
                break;

            case UnauthorizedException ue:
                statusCode = (int)ue.StatusCode;
                message = ue.Message;
                break;

            case CustomException e:
                statusCode = (int)e.StatusCode;
                message = e.Message;
                break;

            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = "Resource not found.";
                break;

            case DomainException:
            case FluentValidation.ValidationException:
            case CustomValidationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "One or more validation errors occurred.";
                break;

            case ArgumentException e:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = e.Message;
                break;

            case InvalidOperationException e:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = e.Message;
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "Something went wrong.";
                break;
        }

        return (statusCode, message);
    }

    public static Dictionary<string, string[]> GetErrors(Exception ex)
    {
        var result = new Dictionary<string, string[]>();
        switch (ex)
        {
            case CustomValidationException customValidationException:
                foreach (var item in customValidationException.Errors)
                {
                    // Many custom validations from FluentValidation has fieldName as empty string. We need to push all of them into field `request`
                    string fieldName = string.IsNullOrWhiteSpace(item.FieldName) ? "request" : item.FieldName;
                    result.TryAdd(fieldName, [item.Message ?? string.Empty]);
                }

                break;

            case DomainException domainException:
                result.TryAdd(domainException.FieldName ?? "request", [domainException.Message]);
                break;

            default:
                break;
        }

        return result;
    }
}