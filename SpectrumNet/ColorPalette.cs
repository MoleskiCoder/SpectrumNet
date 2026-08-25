namespace SpectrumNet
{
    using SDL3;

    internal sealed class ColorPalette : AbstractColorPalette<uint>
    {
        public ColorPalette()
        {
        }

        public void Load(IntPtr hardware)
        {
            this.LoadColour(hardware, Index.Black, 0x00, 0x00, 0x00);
            this.LoadColour(hardware, Index.Blue, 0x00, 0x00, 0xd7);
            this.LoadColour(hardware, Index.Red, 0xd7, 0x00, 0x00);
            this.LoadColour(hardware, Index.Magenta, 0xd7, 0x00, 0xd7);
            this.LoadColour(hardware, Index.Green, 0x00, 0xd7, 0x00);
            this.LoadColour(hardware, Index.Cyan, 0x00, 0xd7, 0xd7);
            this.LoadColour(hardware, Index.Yellow, 0xd7, 0xd7, 0x00);
            this.LoadColour(hardware, Index.White, 0xd7, 0xd7, 0xd7);
        }

        private void LoadColour(IntPtr hardware, Index idx, byte red, byte green, byte blue)
        {
            this.LoadExactColour(hardware, (int)idx, red, green, blue);
            this.LoadExactColour(
                hardware,
                (int)idx + 8,
                (byte)(red > 0 ? red + Bright : 0),
                (byte)(green > 0 ? green + Bright : 0),
                (byte)(blue > 0 ? blue + Bright : 0));
        }

        private void LoadExactColour(IntPtr hardware, int idx, byte red, byte green, byte blue)
        {
            this._colors[idx] = SDL.MapRGBA(hardware, IntPtr.Zero, red, green, blue, (byte)SDL.AlphaOpaque);
        }
    }
}
