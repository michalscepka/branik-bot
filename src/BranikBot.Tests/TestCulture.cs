using System.Globalization;
using System.Runtime.CompilerServices;

namespace BranikBot.Tests;

internal static class TestCulture
{
    // The bot runs with cs-CZ (see Program.cs), and reply texts format money with it.
    [ModuleInitializer]
    internal static void UseCzech()
    {
        var culture = new CultureInfo("cs-CZ");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
