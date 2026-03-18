using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Infrastructure.OpenApi;

[ExcludeFromCodeCoverage]
internal static class ObjectExtensions
{
    public static T? TryGetPropertyValue<T>(this object? obj, string propertyName, T? defaultValue = default) =>
        obj?.GetType().GetRuntimeProperty(propertyName) is { } propertyInfo
            ? (T?)propertyInfo.GetValue(obj)
            : defaultValue;
}

[ExcludeFromCodeCoverage]
public class SwaggerGlobalAuthProcessor(string name) : IOperationProcessor
{
    public SwaggerGlobalAuthProcessor()
        : this(JwtBearerDefaults.AuthenticationScheme)
    {
    }

    public bool Process(OperationProcessorContext context)
    {
        var list = ((AspNetCoreOperationProcessorContext)context).ApiDescription?.ActionDescriptor.TryGetPropertyValue<IList<object>>("EndpointMetadata");
        if (list is null)
        {
            return true;
        }

        if (list.OfType<AllowAnonymousAttribute>().Any())
        {
            return true;
        }

        if (context.OperationDescription.Operation.Security?.Any() != true)
        {
            (context.OperationDescription.Operation.Security ??= new List<OpenApiSecurityRequirement>()).Add(new OpenApiSecurityRequirement
            {
                {
                    name,
                    Array.Empty<string>()
                }
            });
        }

        return true;
    }
}