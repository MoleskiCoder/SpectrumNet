namespace SpectrumNet
{
    internal sealed class NtscTimings : ITimings
    {
        public int LeftRasterBorder { get; } = 32;
        public int RightRasterBorder { get; } = 64;

        public int TopRasterBorder { get; } = 32;
        public int BottomRasterBorder { get; } = 24;

        public float FramesPerSecond { get; } = 59.65f;
        public float MasterClockRate { get; } = 14_110_000.0f;
    }
}
