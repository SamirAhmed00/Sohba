namespace Sohba.Application.Tests.TestDoubles
{
    /// <summary>
    /// Deterministic fake clock for testing time-based token logic
    /// without relying on the machine's real clock or delays.
    /// </summary>
    public sealed class FixedTimeProvider : TimeProvider
    {
        /// <summary>Gets or sets the current fake UTC instant.</summary>
        public DateTimeOffset UtcNowValue { get; set; }

        public FixedTimeProvider(DateTimeOffset? initialTime = null)
        {
            UtcNowValue = initialTime ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => UtcNowValue;

        /// <summary>Advances the fake clock by the given amount.</summary>
        public void Advance(TimeSpan amount) => UtcNowValue += amount;
    }
}
