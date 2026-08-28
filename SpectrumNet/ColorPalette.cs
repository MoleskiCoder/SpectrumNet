namespace SpectrumNet
{
    using Gaming;
    using SDL3;

    internal sealed class ColorPalette : AbstractColorPalette<uint>
    {
        private readonly SDL.PixelFormat _pixelFormat = SDL.PixelFormat.ARGB8888;
        private readonly IntPtr _pixelFormatDetails = IntPtr.Zero;

        public IntPtr PixelFormatDetails => this._pixelFormatDetails;

        public ColorPalette()
        {
            this._pixelFormatDetails = SDL.GetPixelFormatDetails(this._pixelFormat);
            Wrapper.MaybeThrowException(this._pixelFormatDetails, "Unable to obtain pixel format details");
            this.Load();
        }

        private void Load()
        {
            this.LoadColour(Index.Black, 0x00, 0x00, 0x00);
            this.LoadColour(Index.Blue, 0x00, 0x00, 0xd7);
            this.LoadColour(Index.Red, 0xd7, 0x00, 0x00);
            this.LoadColour(Index.Magenta, 0xd7, 0x00, 0xd7);
            this.LoadColour(Index.Green, 0x00, 0xd7, 0x00);
            this.LoadColour(Index.Cyan, 0x00, 0xd7, 0xd7);
            this.LoadColour(Index.Yellow, 0xd7, 0xd7, 0x00);
            this.LoadColour(Index.White, 0xd7, 0xd7, 0xd7);
        }

        private void LoadColour(Index idx, byte red, byte green, byte blue)
        {
            this.LoadExactColour((int)idx, red, green, blue);
            this.LoadExactColour(
                (int)idx + 8,
                (byte)(red > 0 ? red + Bright : 0),
                (byte)(green > 0 ? green + Bright : 0),
                (byte)(blue > 0 ? blue + Bright : 0));
        }

        private void LoadExactColour(int idx, byte red, byte green, byte blue)
        {
            this._colors[idx] = SDL.MapRGBA(this.PixelFormatDetails, IntPtr.Zero, red, green, blue, (byte)SDL.AlphaOpaque);
        }
    }
}
