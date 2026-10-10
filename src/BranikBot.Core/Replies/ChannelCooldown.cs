namespace BranikBot.Replies;

public class ChannelCooldown(TimeSpan cooldown, TimeProvider timeProvider)
{
    private readonly Dictionary<ulong, DateTimeOffset> _startedAt = [];
    private readonly Lock _lock = new();

    // Checks and starts in one step, so two messages arriving together cannot both pass.
    public bool TryStart(ulong channelId)
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (_startedAt.TryGetValue(channelId, out var startedAt) && now - startedAt < cooldown)
                return false;

            _startedAt[channelId] = now;
            return true;
        }
    }
}
