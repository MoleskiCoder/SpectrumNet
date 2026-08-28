namespace SpectrumNet
{
    using SDL3;

    internal sealed class Ula : AbstractUla<uint, SDL.Keycode>
    {
        protected override AbstractColorPalette<uint> Palette => new ColorPalette();

        public Ula(Board bus)
        : base(bus)
        {
            this._borderColour = this.Palette.GetColor(ColorPalette.Index.Black);
        }

        protected override void InitialiseKeyboardMapping()
        {
            // Left side
            this._keyboardMapping[Bit(0)] = [SDL.Keycode.LShift,    SDL.Keycode.Z,      SDL.Keycode.X,      SDL.Keycode.C,      SDL.Keycode.V];
            this._keyboardMapping[Bit(1)] = [SDL.Keycode.A,         SDL.Keycode.S,      SDL.Keycode.D,      SDL.Keycode.F,      SDL.Keycode.G];
            this._keyboardMapping[Bit(2)] = [SDL.Keycode.Q,         SDL.Keycode.W,      SDL.Keycode.E,      SDL.Keycode.R,      SDL.Keycode.T];
            this._keyboardMapping[Bit(3)] = [SDL.Keycode.Alpha1,    SDL.Keycode.Alpha2, SDL.Keycode.Alpha3, SDL.Keycode.Alpha4, SDL.Keycode.Alpha5];

            // Right side
            this._keyboardMapping[Bit(4)] = [SDL.Keycode.Alpha0,    SDL.Keycode.Alpha9, SDL.Keycode.Alpha8, SDL.Keycode.Alpha7, SDL.Keycode.Alpha6];
            this._keyboardMapping[Bit(5)] = [SDL.Keycode.P,         SDL.Keycode.O,      SDL.Keycode.I,      SDL.Keycode.U,      SDL.Keycode.Y];
            this._keyboardMapping[Bit(6)] = [SDL.Keycode.Return,    SDL.Keycode.L,      SDL.Keycode.K,      SDL.Keycode.J,      SDL.Keycode.H];
            this._keyboardMapping[Bit(7)] = [SDL.Keycode.Space,     SDL.Keycode.RShift, SDL.Keycode.M,      SDL.Keycode.N,      SDL.Keycode.B];
        }
    }
}
