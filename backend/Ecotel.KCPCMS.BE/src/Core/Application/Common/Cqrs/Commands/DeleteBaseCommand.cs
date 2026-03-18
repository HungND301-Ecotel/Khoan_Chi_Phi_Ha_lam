using Application.Common.Responses;
using MediatR;

namespace Application.Common.Cqrs.Commands;

public class DeleteBaseCommand<T> : IRequest<ResponseBase<IEnumerable<T>>>
{
    public List<T> Ids { get; init; } = [];
}