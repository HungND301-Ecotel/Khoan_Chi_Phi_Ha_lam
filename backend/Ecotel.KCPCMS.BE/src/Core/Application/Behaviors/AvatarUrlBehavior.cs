using System.Collections;
using Application.Common.Interfaces;
using Application.Dto.Cloud.AWS;
using Application.Interfaces.Infrastructures.Integrates.Cloud.Service.AWS;
using MediatR;

namespace Application.Behaviors;

public class AvatarUrlBehavior<TRequest, TResponse>(IAwsS3Service awsS3Service)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan AvatarLifetime = TimeSpan.FromMinutes(60);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        await ResolveAsync(response);

        return response;
    }

    private async Task ResolveAsync(object? value)
    {
        switch (value)
        {
            case null:
                return;

            case IAvatarCarrier carrier:
                if (!string.IsNullOrEmpty(carrier.AvatarKey))
                {
                    var url = await awsS3Service.GeneratePresignedUrlAsync(
                        carrier.AvatarKey,
                        BucketType.SourceDefault,
                        AvatarLifetime);
                    carrier.SetAvatarUrl(url);
                }
                return;

            case IEnumerable enumerable and not string:
                foreach (var item in enumerable)
                {
                    await ResolveAsync(item);
                }
                return;
        }

        var itemsProperty = value.GetType().GetProperty("Items")
                            ?? value.GetType().GetProperty("Data");

        if (itemsProperty?.GetValue(value) is IEnumerable pagedItems and not string)
        {
            await ResolveAsync(pagedItems);
        }
    }
}