using ForeverBloom.SharedKernel.Result;
using MediatR;

namespace ForeverBloom.Application.Abstractions.Requests;

public interface ICommand : IRequest<Result>, IBaseCommand
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

public interface IBaseCommand
{
}
