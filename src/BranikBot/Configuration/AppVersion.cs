using System.Reflection;

namespace BranikBot.Configuration;

public static class AppVersion
{
    private const string Unknown = "unknown";

    public static string Current { get; } = Parse(
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    // The SDK appends "+<commit>" when it builds inside a git checkout.
    public static string Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
            return Unknown;

        var metadataStart = informationalVersion.IndexOf('+');
        return metadataStart < 0 ? informationalVersion : informationalVersion[..metadataStart];
    }
}
