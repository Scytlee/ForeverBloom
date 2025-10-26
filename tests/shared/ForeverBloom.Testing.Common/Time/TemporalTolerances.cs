namespace ForeverBloom.Testing.Common.Time;

public static class TemporalTolerances
{
    // PostgreSQL timestamp(6) precision is microseconds; use as assertion tolerance after DB round-trips.
    public static readonly TimeSpan DatabaseTimestamp = TimeSpan.FromMicroseconds(1);
}
