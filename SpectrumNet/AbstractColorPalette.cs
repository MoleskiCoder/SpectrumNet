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

        public ColorT GetColor(int index, bool bright) => this.GetColor(bright ? index + 8 : index);

        public ColorT GetColor(int index) => this._colors[index];

        public ColorT GetColor(Index index) => this.GetColor((int)index);
    }
}
