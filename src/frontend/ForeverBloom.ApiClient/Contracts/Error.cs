namespace ForeverBloom.ApiClient.Contracts;

public class Error
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public string Code { get; }
    public string Message { get; }
    public Exception? Exception { get; }

    public Error(string code, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Exception = exception;
    }

    public static implicit operator Error(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Error("General.Exception", exception.Message, exception);
    }

    public override string ToString() =>
      $"Error ({Code}): {Message}"
      + (Exception is not null
        ? $" (Exception: {Exception.GetType().Name})"
        : string.Empty);
}
