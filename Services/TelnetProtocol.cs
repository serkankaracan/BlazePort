namespace BlazePort.Services;

/// <summary>
/// Shared telnet protocol helpers: IAC parsing and option negotiation.
/// Used by both PortScanner (one-shot banner reads) and TelnetClient (interactive sessions).
/// </summary>
internal static class TelnetProtocol
{
    public const byte IAC  = 0xFF;
    public const byte WILL = 0xFB;
    public const byte WONT = 0xFC;
    public const byte DO   = 0xFD;
    public const byte DONT = 0xFE;
    public const byte SB   = 0xFA;
    public const byte SE   = 0xF0;

    private const byte OPT_ECHO        = 1;
    private const byte OPT_SUPPRESS_GA = 3;

    /// <summary>
    /// Parses raw bytes, separates clean text output from IAC negotiation responses.
    /// </summary>
    public static (byte[] CleanOutput, byte[] IacResponses) ParseIac(byte[] data, int length)
    {
        var output = new List<byte>(length);
        var responses = new List<byte>();

        for (int i = 0; i < length; i++)
        {
            if (data[i] != IAC)
            {
                output.Add(data[i]);
                continue;
            }

            if (i + 1 >= length) break;
            var cmd = data[i + 1];

            if (cmd == IAC)
            {
                output.Add(IAC);
                i++;
                continue;
            }

            if (cmd is WILL or WONT or DO or DONT)
            {
                if (i + 2 >= length) break;
                var opt = data[i + 2];

                var response = Negotiate(cmd, opt);
                if (response is not null)
                    responses.AddRange(response);

                i += 2;
                continue;
            }

            if (cmd == SB)
            {
                i++;
                while (i + 1 < length)
                {
                    i++;
                    if (data[i] == IAC && i + 1 < length && data[i + 1] == SE)
                    {
                        i++;
                        break;
                    }
                }
                continue;
            }

            i++;
        }

        return (output.ToArray(), responses.ToArray());
    }

    /// <summary>
    /// Responds to WILL/DO negotiations.
    /// Accepts ECHO and SUPPRESS-GO-AHEAD, refuses everything else.
    /// </summary>
    private static byte[]? Negotiate(byte command, byte option)
    {
        return command switch
        {
            DO when option is OPT_SUPPRESS_GA => [IAC, WILL, option],
            DO   => [IAC, WONT, option],

            WILL when option is OPT_ECHO        => [IAC, DO, option],
            WILL when option is OPT_SUPPRESS_GA => [IAC, DO, option],
            WILL => [IAC, DONT, option],

            _ => null
        };
    }
}
