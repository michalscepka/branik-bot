namespace BranikBot.Replies;

public class ChannelCooldown(TimeSpan cooldown, TimeProvider timeProvider)
{
    private readonly Dictionary<ulong, DateTimeOffset> _startedAt = [];
    private readonly Lock _lock = new();

    // A cheap early exit only. TryStart is what decides whether a reply may be sent.
    public bool IsActive(ulong channelId)
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            return IsActive(channelId, now);
        }
    }

    // Checks and starts in one step, so two messages arriving together cannot both pass.
    public bool TryStart(ulong channelId)
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (IsActive(channelId, now))
                return false;

            _startedAt[channelId] = now;
            return true;
        }
    }

    private bool IsActive(ulong channelId, DateTimeOffset now) =>
        _startedAt.TryGetValue(channelId, out var startedAt) && now - startedAt < cooldown;
}
