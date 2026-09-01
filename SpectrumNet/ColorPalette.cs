namespace SpectrumNet
{
    using Gaming;
    using SDL3;

    internal sealed class ColorPalette : AbstractColorPalette<uint>
    {
        public const SDL.PixelFormat PixelFormat = SDL.PixelFormat.ARGB8888;

        public IntPtr PixelFormatDetails { get; }

        public ColorPalette()
        {
            this.PixelFormatDetails = SDL.GetPixelFormatDetails(PixelFormat);
            Wrapper.MaybeThrowException(this.PixelFormatDetails, "Unable to obtain pixel format details");
            this.Load();
        }

        protected override uint ExactColour(byte red, byte green, byte blue) => SDL.MapRGBA(this.PixelFormatDetails, IntPtr.Zero, red, green, blue, (byte)SDL.AlphaOpaque);
    }
}
