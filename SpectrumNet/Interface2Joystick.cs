namespace SpectrumNet
{
    using SDL3;

    internal sealed class Interface2Joystick(Board motherboard) : Joystick(motherboard)
    {
        public override void PushUp() => this.BUS.ULA.PokeKey(SDL.Keycode.Alpha4);

        public override void PushDown() => this.BUS.ULA.PokeKey(SDL.Keycode.Alpha3);

        public override void PushLeft() => this.BUS.ULA.PokeKey(SDL.Keycode.Alpha1);

        public override void PushRight() => this.BUS.ULA.PokeKey(SDL.Keycode.Alpha2);

        public override void PushFire() => this.BUS.ULA.PokeKey(SDL.Keycode.Alpha5);

        public override void ReleaseUp() => this.BUS.ULA.PullKey(SDL.Keycode.Alpha4);

        public override void ReleaseDown() => this.BUS.ULA.PullKey(SDL.Keycode.Alpha3);

        public override void ReleaseLeft() => this.BUS.ULA.PullKey(SDL.Keycode.Alpha1);

        public override void ReleaseRight() => this.BUS.ULA.PullKey(SDL.Keycode.Alpha2);

        public override void ReleaseFire() => this.BUS.ULA.PullKey(SDL.Keycode.Alpha5);
    }
}
