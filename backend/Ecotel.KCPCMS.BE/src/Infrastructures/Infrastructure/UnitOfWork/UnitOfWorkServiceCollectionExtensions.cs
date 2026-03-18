using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Infrastructure.CustomRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.UnitOfWork;

public static class UnitOfWorkServiceCollectionExtensions
{

    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IWriteRepositoryFactory, UnitOfWork<TContext>>();

        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddScoped<IUnitOfWork<TContext>, UnitOfWork<TContext>>();

        return services;
    }
    public static IServiceCollection AddUnitOfWork<TContext1, TContext2>(this IServiceCollection services)
        where TContext1 : DbContext
        where TContext2 : DbContext
    {
        services.AddScoped<IUnitOfWork<TContext1>, UnitOfWork<TContext1>>();
        services.AddScoped<IUnitOfWork<TContext2>, UnitOfWork<TContext2>>();

        return services;
    }

    public static IServiceCollection AddUnitOfWork<TContext1, TContext2, TContext3>(this IServiceCollection services)
        where TContext1 : DbContext
        where TContext2 : DbContext
        where TContext3 : DbContext
    {
        services.AddScoped<IUnitOfWork<TContext1>, UnitOfWork<TContext1>>();
        services.AddScoped<IUnitOfWork<TContext2>, UnitOfWork<TContext2>>();
        services.AddScoped<IUnitOfWork<TContext3>, UnitOfWork<TContext3>>();

        return services;
    }

    public static IServiceCollection AddUnitOfWork<TContext1, TContext2, TContext3, TContext4>(this IServiceCollection services)
        where TContext1 : DbContext
        where TContext2 : DbContext
        where TContext3 : DbContext
        where TContext4 : DbContext
    {
        services.AddScoped<IUnitOfWork<TContext1>, UnitOfWork<TContext1>>();
        services.AddScoped<IUnitOfWork<TContext2>, UnitOfWork<TContext2>>();
        services.AddScoped<IUnitOfWork<TContext3>, UnitOfWork<TContext3>>();
        services.AddScoped<IUnitOfWork<TContext4>, UnitOfWork<TContext4>>();

        return services;
    }

    public static IServiceCollection AddCustomRepository(this IServiceCollection services)
    {
        services.AddScoped<IUserRoleCustomRepository, UserRoleCustomRepository>();
        services.AddScoped<IUserCustomRepository, UserCustomRepository>();
        return services;
    }
}