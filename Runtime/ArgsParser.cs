namespace BlazePort.Runtime
{
    internal static class ArgsParser
    {
        public static AppArgs Parse(string[]? args)
        {
            // Default mode is Client
            var selectedMode = AppMode.Client;
            string? lastWarning = null;

            // If no arguments are provided, return default AppArgs
            if (args == null || args.Length == 0)
            {
                return new AppArgs(selectedMode);
            }

            // Iterate through the arguments to find "--mode"
            for (int i = 0; i < args.Length; i++)
            {
                // Check if the current argument is "--mode" (case-insensitive)
                if (!args[i].Equals("--mode", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Ensure that there is a value following the "--mode" argument
                if (i + 1 >= args.Length)
                {
                    lastWarning = "Hata: '--mode' parametresi için bir değer belirtilmedi. Varsayılan: Client.";
                    break;
                }

                // Get the raw value for the mode argument
                var rawValue = args[i + 1].Trim();

                // Try to parse the raw value into the AppMode enum (case-insensitive)
                if (Enum.TryParse<AppMode>(rawValue, ignoreCase: true, out var parsedMode))
                {
                    // Last valid value wins
                    selectedMode = parsedMode;
                }
                else
                {
                    lastWarning = $"Geçersiz mod: '{rawValue}'. Geçerli modlar: Client, Server, Admin.";
                }

                // Skip the next argument since it's the value for "--mode"
                i++;
            }

            return new AppArgs(selectedMode, lastWarning);
        }
    }
}