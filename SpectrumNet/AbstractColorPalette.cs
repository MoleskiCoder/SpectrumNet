namespace SpectrumNet
{
    internal abstract class AbstractColorPalette<ColorT>
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

        protected readonly ColorT[] _colors = new ColorT[16];

        protected AbstractColorPalette()
        {
        }

        public ColorT GetColor(int index, bool bright = false) => this.GetColor(bright ? index + 8 : index);

        public ColorT GetColor(int index) => this._colors[index];

        public ColorT GetColor(Index index) => this.GetColor((int)index);

        protected abstract ColorT ExactColour(byte red, byte green, byte blue);

        protected void LoadExactColour(int idx, byte red, byte green, byte blue) => this._colors[idx] = this.ExactColour(red, green, blue);

        protected void Load()
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

        protected void LoadColour(Index idx, byte red, byte green, byte blue)
        {
            this.LoadExactColour((int)idx, red, green, blue);
            this.LoadExactColour(
                (int)idx + 8,
                (byte)(red > 0 ? red + Bright : 0),
                (byte)(green > 0 ? green + Bright : 0),
                (byte)(blue > 0 ? blue + Bright : 0));
        }
    }
}
