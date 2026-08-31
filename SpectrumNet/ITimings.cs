namespace SpectrumNet
{
    internal interface ITimings
    {
        public abstract int LeftRasterBorder { get; }
        public abstract int RightRasterBorder { get; }

        public abstract int TopRasterBorder { get; }
        public abstract int BottomRasterBorder { get; }

        public abstract float FramesPerSecond { get; }
        public abstract float MasterClockRate { get; }

        public float UlaClockRate => this.MasterClockRate / 2.0f;
        public float CpuClockRate => this.UlaClockRate / 2.0f;
    }
}
