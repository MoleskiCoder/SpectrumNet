namespace SpectrumNet
{
    internal sealed class Configuration
    {
        public bool DebugMode { get; set; }

        public bool VerboseMode { get; set; } //= true;

        public bool ProfileMode { get; set; }

        public bool DrawGraphics { get; set; } = true;

        public string RomDirectory { get; } = "roms";

        public string ProgramDirectory { get; } = "programs";
    }
}
