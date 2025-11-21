using ForeverBloom.Application.Abstractions.Time;

namespace ForeverBloom.Testing.Integration.Time;

public sealed class TestTimeProvider : ITimeProvider
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets the initial timestamp assigned when the provider was created.
    /// </summary>
    public DateTimeOffset InitialTime { get; private init; }

    /// <summary>
    /// Gets the latest clock value stored by the provider.
    /// </summary>
    public DateTimeOffset CurrentTime
    {
        get;
        private set
        {
            if (value < field)
            {
                throw new InvalidOperationException("TimeProvider cannot move backwards in time.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets the increment applied to the clock when advancing automatically.
    /// </summary>
    public TimeSpan AdvanceBy { get; }

    private static readonly TimeSpan DefaultAdvanceBy = TimeSpan.FromMicroseconds(10);

    /// <summary>
    /// Gets a value indicating whether the provider advances automatically
    /// after <see cref="UtcNow"/> or unfreezing.
    /// </summary>
    public bool AutoAdvance { get; private set; }

    public TestTimeProvider(
        DateTimeOffset initialTime,
        TimeSpan? advanceBy = null,
        bool autoAdvance = true)
    {
        if (advanceBy is not null && advanceBy <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("AdvanceBy must be a positive timespan.");
        }

        InitialTime = initialTime;
        CurrentTime = initialTime;
        AdvanceBy = advanceBy ?? DefaultAdvanceBy;
        AutoAdvance = autoAdvance;
    }

    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC) as provided by this time provider.
    /// </summary>
    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_lock)
            {
                var current = CurrentTime;
                if (AutoAdvance)
                {
                    CurrentTime = current + AdvanceBy;
                }

                return current;
            }
        }
    }

    /// <summary>
    /// Stops the automatic advancement of time and returns the current time.
    /// </summary>
    /// <returns>The current time at the moment of calling this method.</returns>
    public DateTimeOffset Freeze()
    {
        AutoAdvance = false;
        return CurrentTime;
    }

    /// <summary>
    /// Stops the automatic advancement of time and sets the current time to the specified value.
    /// </summary>
    /// <param name="time">The time to set as the current time.</param>
    public void FreezeAt(DateTimeOffset time)
    {
        lock (_lock)
        {
            AutoAdvance = false;
            CurrentTime = time;
        }
    }

    /// <summary>
    /// Resumes the automatic advancement of time, adjusting the current time forward by the predefined increment.
    /// </summary>
    public void Unfreeze()
    {
        lock (_lock)
        {
            AutoAdvance = true;
            CurrentTime += AdvanceBy;
        }
    }

    /// <summary>
    /// Moves the clock forward by the specified duration.
    /// </summary>
    public void FastForwardBy(TimeSpan timespan)
    {
        if (timespan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timespan), "TimeProvider cannot rewind time.");
        }

        lock (_lock)
        {
            CurrentTime += timespan;
        }
    }
}
