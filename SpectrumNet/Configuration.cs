namespace SpectrumNet
{
    using SDL3;

    internal sealed class Configuration
    {
        public ITimings Timings { get; } = new PalTimings();
        //public ITimings Timings { get; } = new NtscTimings();

        public bool DebugMode { get; set; }

        //public EightBit.ILogger.LogLevel LoggingLevel { get; set; } = EightBit.ILogger.LogLevel.Debugging;
        public EightBit.ILogger.LogLevel LoggingLevel { get; set; } = EightBit.ILogger.LogLevel.Information;
        //public EightBit.ILogger.LogLevel LoggingLevel { get; set; } = EightBit.ILogger.LogLevel.Warning;

        public bool ProfileMode { get; set; }

        public bool DrawGraphics { get; set; } = true;

        public string RomDirectory { get; } = "roms";

        public string ProgramDirectory { get; } = "programs";
    }
}
