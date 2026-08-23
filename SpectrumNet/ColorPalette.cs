namespace SpectrumNet
{
    using SDL3;

    internal sealed class ColorPalette
    {
        internal enum Index
        {
            Black,
            Blue,
            Red,
            Magenta,
            Green,
            Cyan,
            Yellow,
            White
        }

        public const int Bright = 0x28;

        private readonly uint[] _colors = new uint[16];

        public ColorPalette()
        {
        }

        public uint GetColor(int index, bool bright) => this.GetColor(bright ? index + 8 : index);

        public uint GetColor(Index index, bool bright) => this.GetColor((int)index, bright);

        public uint GetColor(int index) => this._colors[index];

        public uint GetColor(Index index) => this.GetColor((int)index);

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
