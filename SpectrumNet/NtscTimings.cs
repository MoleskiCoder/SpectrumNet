namespace SpectrumNet
{
    internal sealed class NtscTimings : ITimings
    {
        public int TopRasterBorder { get; } = 32;
        public int BottomRasterBorder { get; } = 32;

        public float MasterClockRate { get; } = 14_110_000.0f;
    }
}
