using Application.Common.Responses;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Host.Controllers.Base;

#nullable disable
#pragma warning disable RCS1163, IDE0060

public static class ApiConventions
{
    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Search(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Get()
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Get(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Get(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object cancellationtoken)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Post(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Post(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object cancellationToken)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Register(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Create(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Update(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Update(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Update(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object cancellationToken)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Put(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Put(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object cancellationToken)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Delete(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Delete(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object cancellationToken)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }

    [ProducesResponseType(200, Type = typeof(ResponseBase))]
    [ProducesResponseType(400, Type = typeof(ErrorResult))]
    [ProducesDefaultResponseType(typeof(ErrorResult))]
    [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
    public static void Generate(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request)
    {
        // This method is intentionally empty.
        // It's an API convention method used only for Swagger/OpenAPI documentation.
    }
}