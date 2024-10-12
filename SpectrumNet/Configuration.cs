namespace SpectrumNet
{
    internal class Configuration
    {
        public bool DebugMode { get; set; }

        public bool ProfileMode { get; set; }

        public bool DrawGraphics { get; set; } = true;

        public string RomDirectory { get; } = "roms";

        public string ProgramDirectory { get; } = "programs";
    }
}
