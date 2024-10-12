namespace SpectrumNet
{
    using Microsoft.Xna.Framework;

    internal class ColorPalette
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

        private readonly Color[] colors = new Color[16];

        public ColorPalette()
        {
        }

        public Color GetColor(int index, bool bright) => this.GetColor(bright ? index + 8 : index);

        public Color GetColor(Index index, bool bright) => this.GetColor((int)index, bright);

        public Color GetColor(int index) => this.colors[index];

        public Color GetColor(Index index) => this.GetColor((int)index);

        public void Load()
        {
            this.LoadColour((int)Index.Black, 0x00, 0x00, 0x00);
            this.LoadColour((int)Index.Blue, 0x00, 0x00, 0xd7);
            this.LoadColour((int)Index.Red, 0xd7, 0x00, 0x00);
            this.LoadColour((int)Index.Magenta, 0xd7, 0x00, 0xd7);
            this.LoadColour((int)Index.Green, 0x00, 0xd7, 0x00);
            this.LoadColour((int)Index.Cyan, 0x00, 0xd7, 0xd7);
            this.LoadColour((int)Index.Yellow, 0xd7, 0xd7, 0x00);
            this.LoadColour((int)Index.White, 0xd7, 0xd7, 0xd7);
        }

        private void LoadColour(int idx, int red, int green, int blue)
        {
            this.LoadExactColour(idx, red, green, blue);
            this.LoadExactColour(
                idx + 8,
                red > 0 ? red + Bright : 0,
                green > 0 ? green + Bright : 0,
                blue > 0 ? blue + Bright : 0);
        }

        private void LoadExactColour(int idx, int red, int green, int blue) => this.colors[idx] = new Color(red, green, blue);
    }
}
