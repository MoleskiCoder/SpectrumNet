namespace SpectrumNet
{
    using SDL3;

    internal sealed class Configuration
    {
        public bool DebugMode { get; set; }

        //public SDL.LogPriority LoggingLevel { get; set; } = SDL.LogPriority.Debug;
        public SDL.LogPriority LoggingLevel { get; set; } = SDL.LogPriority.Info;
        //public SDL.LogPriority LoggingLevel { get; set; } = SDL.LogPriority.Warn;

        public bool ProfileMode { get; set; }

        public bool DrawGraphics { get; set; } = true;

        public string RomDirectory { get; } = "roms";

        public string ProgramDirectory { get; } = "programs";
    }
}
