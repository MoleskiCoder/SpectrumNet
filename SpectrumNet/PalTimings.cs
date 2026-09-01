namespace SpectrumNet
{
    internal sealed class PalTimings : ITimings
    {
        public int LeftRasterBorder { get; } = 32;
        public int RightRasterBorder { get; } = 64;

        public int TopRasterBorder { get; } = 56;
        public int BottomRasterBorder { get; } = 56;

        public float MasterClockRate { get; } = 14_000_000.0f;
    }
}
