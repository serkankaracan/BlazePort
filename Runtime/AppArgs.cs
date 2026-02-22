namespace BlazePort.Runtime
{
    internal sealed class AppArgs
    {
        public AppMode Mode { get; }
        public string? Warning { get; }

        public AppArgs(AppMode mode, string? warning = null)
        {
            Mode = mode;
            Warning = warning;
        }
    }
}
