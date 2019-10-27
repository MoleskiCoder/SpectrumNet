namespace SpectrumNet
{
    using Microsoft.Xna.Framework;

    public class ColourPalette
    {
        enum Indices
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

        private readonly Color[] colours = new Color[16];

        public ColourPalette()
        {
        }

        public Color GetColour(int index, bool bright) => this.GetColour(bright ? index + 8 : index);

        public Color GetColour(int index) => this.colours[index];

        public void Load()
        {
            this.LoadColour((int)Indices.Black, 0x00, 0x00, 0x00);
            this.LoadColour((int)Indices.Blue, 0x00, 0x00, 0xd7);
            this.LoadColour((int)Indices.Red, 0xd7, 0x00, 0x00);
            this.LoadColour((int)Indices.Magenta, 0xd7, 0x00, 0xd7);
            this.LoadColour((int)Indices.Green, 0x00, 0xd7, 0x00);
            this.LoadColour((int)Indices.Cyan, 0x00, 0xd7, 0xd7);
            this.LoadColour((int)Indices.Yellow, 0xd7, 0xd7, 0x00);
            this.LoadColour((int)Indices.White, 0xd7, 0xd7, 0xd7);
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

        private void LoadExactColour(int idx, int red, int green, int blue) => this.colours[idx] = new Color(red, green, blue);
    }
}
