using ForeverBloom.SharedKernel.Result;
using MediatR;

namespace ForeverBloom.Application.Abstractions.Requests;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
