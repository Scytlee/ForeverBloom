namespace ForeverBloom.SharedKernel.Result;

/// <summary>
/// Marker interface for Result types, enabling UnitOfWorkBehavior to inspect success state.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    IError? Error { get; }
}
