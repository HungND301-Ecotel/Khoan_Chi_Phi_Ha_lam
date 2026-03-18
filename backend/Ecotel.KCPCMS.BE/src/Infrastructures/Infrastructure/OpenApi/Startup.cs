using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.Diagnostics.CodeAnalysis;
using ZymLabs.NSwag.FluentValidation;

namespace Infrastructure.OpenApi;

[ExcludeFromCodeCoverage]
internal static class Startup
{
    internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();
        if (settings == null)
        {
            return services;
        }

        if (!settings.Enable)
        {
            return services;
        }

        services.AddApiVersioning(
                options =>
                {
                    // reporting api versions will return the headers
                    // "api-supported-versions" and "api-deprecated-versions"
                    options.ReportApiVersions = true;

                    options.Policies.Sunset(0.9)
                        .Effective(DateTimeOffset.Now.AddDays(60))
                        .Link("policy.html")
                        .Title("Versioning Policy")
                        .Type("text/html");
                })
            .AddMvc()
            .AddApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                });

        services.AddEndpointsApiExplorer();

        services.AddScoped<FluentValidationSchemaProcessor>(provider =>
        {
            var validationRules = provider.GetService<IEnumerable<FluentValidationRule>>();
            var loggerFactory = provider.GetService<ILoggerFactory>();

            return new FluentValidationSchemaProcessor(provider, validationRules, loggerFactory);
        });

        _ = services.AddOpenApiDocument((document, _) =>
        {
            document.PostProcess = doc =>
            {
                doc.Info.Title = settings.Title;
                doc.Info.Version = settings.Version;
                doc.Info.Description = settings.Description;
                doc.Info.Contact = new OpenApiContact
                {
                    Name = settings.ContactName,
                    Email = settings.ContactEmail,
                    Url = settings.ContactUrl
                };
                doc.Info.License = new OpenApiLicense
                {
                    Name = settings.LicenseName,
                    Url = settings.LicenseUrl
                };
            };

            if (config["SecuritySettings:Provider"]!.Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
            {
                document.AddSecurity(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Flow = OpenApiOAuth2Flow.AccessCode,
                    Description = "OAuth2.0 Auth Code with PKCE",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = config["SecuritySettings:Swagger:AuthorizationUrl"],
                            TokenUrl = config["SecuritySettings:Swagger:TokenUrl"],
                            Scopes = new Dictionary<string, string>
                            {
                                { config["SecuritySettings:Swagger:ApiScope"]!, "access the api" }
                            }
                        }
                    }
                });
            }
            else
            {
                document.AddSecurity(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Input your Bearer token to access this API",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });
            }

            document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());
            document.OperationProcessors.Add(new SwaggerGlobalAuthProcessor());

            document.OperationProcessors.Add(new SwaggerHeaderAttributeProcessor());
        });

        return services;
    }

    internal static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app, IConfiguration config, IHostEnvironment environment)
    {
        if (!config.GetValue<bool>("SwaggerSettings:Enable"))
        {
            return app;
        }

        app.UseOpenApi(options =>
        {
            options.PostProcess = (document, _) =>
            {
                string serverUrl = config.GetValue<string>("SwaggerSettings:BaseUrl") ?? string.Empty;
                var defaultServer = document.Servers.FirstOrDefault();
                if (string.IsNullOrEmpty(serverUrl))
                {
                    return;
                }

                document.Servers.Clear();
                document.Servers.Add(environment.IsProduction()
                    ? new OpenApiServer { Url = serverUrl }
                    : defaultServer);
            };
        });

        app.UseSwaggerUi(options =>
        {
            options.DefaultModelsExpandDepth = -1;
            options.DocExpansion = "none";
            options.TagsSorter = "alpha";
        });

        return app;
    }
}