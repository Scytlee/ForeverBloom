using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.SharedKernel.UnitTests.Result;

internal sealed record TestError(string Code, string Message) : IError;
