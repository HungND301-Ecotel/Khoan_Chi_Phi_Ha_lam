using Application.Common.Services;
using Application.Configurations;
using Application.Identity.Tokens;
using Application.Interfaces;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using Application.Interfaces.Infrastructures.Integrates.External.Service.Email;
using Application.Interfaces.Services;
using External.Service.Email;
using Infrastructure.Auth.Authorization;
using Infrastructure.Services.Cache;
using Infrastructure.Services.Catalog;
using Infrastructure.Services.Identity;
using Infrastructure.Services.Integrates;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

internal static class Startup
{
    internal static IServiceCollection AddRegisterService(this IServiceCollection services)
    {
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<IInitializeCacheService, InitializeCacheService>();
        services.AddTransient<IAwsCloudService, AwsCloudService>();

        services.AddTransient<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddTransient<WebsiteInfoConfiguration>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<IEmailService, EmailService>();

        services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IVerificationService, VerificationService>();

        services.AddTransient<ICodeService, CodeService>();
        services.AddTransient<ICostService, CostService>();
        services.AddTransient<IExcelService, ExcelService>();

        return services;
    }
}