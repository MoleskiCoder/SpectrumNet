namespace SpectrumNet
{
    internal interface ITimings
    {
        public const int ActiveRasterWidth = 256;
        public const int ActiveRasterHeight = 192;

        public const int HorizontalRetraceClocks = 96;
        public const int VerticalRetraceLines = 8;

        public abstract int LeftRasterBorder { get; }
        public abstract int RightRasterBorder { get; }

        public abstract int TopRasterBorder { get; }
        public abstract int BottomRasterBorder { get; }

        public abstract float MasterClockRate { get; }

        public float UlaClockRate => this.MasterClockRate / 2.0f;
        public float CpuClockRate => this.UlaClockRate / 2.0f;

        public int PowerOnResetCycles => 1; // (int)CpuClockRate / 10;

        public int RasterWidth => this.LeftRasterBorder + ActiveRasterWidth + this.RightRasterBorder;
        public int RasterHeight => this.TopRasterBorder + ActiveRasterHeight + this.BottomRasterBorder;
        public int TotalHeight => VerticalRetraceLines + this.RasterHeight;

        public int TotalHorizontalClocks => HorizontalRetraceClocks + this.RasterWidth;
        public int TotalClocks => this.TotalHorizontalClocks * this.TotalHeight;

        public float FramesPerSecond => this.UlaClockRate / this.TotalClocks;
    }
}
