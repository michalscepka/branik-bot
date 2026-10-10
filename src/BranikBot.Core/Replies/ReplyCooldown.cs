namespace BranikBot.Replies;

public class ReplyCooldown(TimeSpan cooldown, TimeProvider timeProvider)
{
    private readonly Dictionary<ulong, DateTimeOffset> _startedAt = [];
    private readonly Lock _lock = new();

    public bool IsActive(ulong guildId)
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            return IsActive(guildId, now);
        }
    }

    public bool TryStart(ulong guildId)
    {
        var now = timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (IsActive(guildId, now))
                return false;

            _startedAt[guildId] = now;
            return true;
        }
    }

    private bool IsActive(ulong guildId, DateTimeOffset now) =>
        _startedAt.TryGetValue(guildId, out var startedAt) && now - startedAt < cooldown;
}
