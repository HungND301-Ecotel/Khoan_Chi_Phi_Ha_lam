using Application.Catalog.Index.Metrics.Commands;
using Application.Catalog.Index.Metrics.Queries;
using Application.Common.Models;
using Application.Dto.Catalog.Metric;
using Domain.Entities.Index;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Catalog.Index.Metrics
{
    public static class Startup
    {
        public static IServiceCollection AddMetricMediarCqrs(this IServiceCollection services)
        {
            //Get All Metric
            services.AddTransient<IRequestHandler<GetAllMetricQuery<Hardness>, PaginationResponse<MetricDto>>, GetAllMetricQueryHandler<Hardness>>();
            services.AddTransient<IRequestHandler<GetAllMetricQuery<InsertItem>, PaginationResponse<MetricDto>>, GetAllMetricQueryHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<GetAllMetricQuery<SupportStep>, PaginationResponse<MetricDto>>, GetAllMetricQueryHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<GetAllMetricQuery<Technology>, PaginationResponse<MetricDto>>, GetAllMetricQueryHandler<Technology>>();
            services.AddTransient<IRequestHandler<GetAllMetricQuery<SeamFace>, PaginationResponse<MetricDto>>, GetAllMetricQueryHandler<SeamFace>>();

            // Get Metric By Id
            services.AddTransient<IRequestHandler<GetMetricByIdQuery<Hardness>, MetricDto>, GetMetricByIdQueryHandler<Hardness>>();
            services.AddTransient<IRequestHandler<GetMetricByIdQuery<InsertItem>, MetricDto>, GetMetricByIdQueryHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<GetMetricByIdQuery<SupportStep>, MetricDto>, GetMetricByIdQueryHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<GetMetricByIdQuery<Technology>, MetricDto>, GetMetricByIdQueryHandler<Technology>>();
            services.AddTransient<IRequestHandler<GetMetricByIdQuery<SeamFace>, MetricDto>, GetMetricByIdQueryHandler<SeamFace>>();

            // Create Metric
            services.AddTransient<IRequestHandler<CreateMetricCommand<Hardness>, bool>, CreateMetricCommandHandler<Hardness>>();
            services.AddTransient<IRequestHandler<CreateMetricCommand<InsertItem>, bool>, CreateMetricCommandHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<CreateMetricCommand<SupportStep>, bool>, CreateMetricCommandHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<CreateMetricCommand<Technology>, bool>, CreateMetricCommandHandler<Technology>>();
            services.AddTransient<IRequestHandler<CreateMetricCommand<SeamFace>, bool>, CreateMetricCommandHandler<SeamFace>>();

            // Update Metric
            services.AddTransient<IRequestHandler<UpdateMetricCommand<Hardness>, bool>, UpdateMetricCommandHandler<Hardness>>();
            services.AddTransient<IRequestHandler<UpdateMetricCommand<InsertItem>, bool>, UpdateMetricCommandHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<UpdateMetricCommand<SupportStep>, bool>, UpdateMetricCommandHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<UpdateMetricCommand<Technology>, bool>, UpdateMetricCommandHandler<Technology>>();
            services.AddTransient<IRequestHandler<UpdateMetricCommand<SeamFace>, bool>, UpdateMetricCommandHandler<SeamFace>>();

            // Delete Metric
            services.AddTransient<IRequestHandler<DeleteMetricCommand<Hardness>, bool>, DeleteMetricCommandHandler<Hardness>>();
            services.AddTransient<IRequestHandler<DeleteMetricCommand<InsertItem>, bool>, DeleteMetricCommandHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<DeleteMetricCommand<SupportStep>, bool>, DeleteMetricCommandHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<DeleteMetricCommand<Technology>, bool>, DeleteMetricCommandHandler<Technology>>();
            services.AddTransient<IRequestHandler<DeleteMetricCommand<SeamFace>, bool>, DeleteMetricCommandHandler<SeamFace>>();

            // Delete List Metric
            services.AddTransient<IRequestHandler<DeleteMetricListCommand<Hardness>, bool>, DeleteMetricListCommandHandler<Hardness>>();
            services.AddTransient<IRequestHandler<DeleteMetricListCommand<InsertItem>, bool>, DeleteMetricListCommandHandler<InsertItem>>();
            services.AddTransient<IRequestHandler<DeleteMetricListCommand<SupportStep>, bool>, DeleteMetricListCommandHandler<SupportStep>>();
            services.AddTransient<IRequestHandler<DeleteMetricListCommand<Technology>, bool>, DeleteMetricListCommandHandler<Technology>>();
            services.AddTransient<IRequestHandler<DeleteMetricListCommand<SeamFace>, bool>, DeleteMetricListCommandHandler<SeamFace>>();
            return services;
        }
    }
}
