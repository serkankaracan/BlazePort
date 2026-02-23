namespace BlazePort.Runtime;

internal static class ArgsParser
{
    public static AppArgs Parse(string[]? args)
    {
        var selectedMode = AppMode.Client;
        string? lastWarning = null;

        if (args is null or { Length: 0 })
            return new AppArgs(selectedMode);

        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].Equals("--mode", StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 >= args.Length)
            {
                lastWarning = "No value specified for '--mode'. Defaulting to Client.";
                break;
            }

            var rawValue = args[i + 1].Trim();

            if (Enum.TryParse<AppMode>(rawValue, ignoreCase: true, out var parsedMode))
                selectedMode = parsedMode;
            else
                lastWarning = $"Invalid mode: '{rawValue}'. Valid modes: Client, Server, Admin.";

            i++;
        }

        return new AppArgs(selectedMode, lastWarning);
    }
}
