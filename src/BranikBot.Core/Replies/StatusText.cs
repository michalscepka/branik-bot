namespace BranikBot.Replies;

public static class StatusText
{
    // Without a stored price there is only the version to show.
    public static string Create(decimal? price, string version)
    {
        return price is null
            ? string.Format(Messages.StatusWithoutPrice, version)
            : string.Format(Messages.Status, price, version);
    }
}
