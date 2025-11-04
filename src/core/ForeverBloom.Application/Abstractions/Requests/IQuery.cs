using ForeverBloom.SharedKernel.Result;
using MediatR;

namespace ForeverBloom.Application.Abstractions.Requests;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
