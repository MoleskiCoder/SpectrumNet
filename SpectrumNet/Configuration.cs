namespace SpectrumNet
{
    public class Configuration
    {
        public bool DebugMode { get; set; } = false;

        public bool ProfileMode { get; set; } = false;

        public bool DrawGraphics { get; set; } = true;

        public string RomDirectory { get; } = "roms";

        public string ProgramDirectory { get; } = "programs";
    }
}
