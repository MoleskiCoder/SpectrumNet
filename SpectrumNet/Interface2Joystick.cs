namespace SpectrumNet
{
    using Microsoft.Xna.Framework.Input;

    internal sealed class Interface2Joystick(Board motherboard) : Joystick(motherboard)
    {
        public override void PushUp() => this.BUS.ULA.PokeKey(Keys.D4);

        public override void PushDown() => this.BUS.ULA.PokeKey(Keys.D3);

        public override void PushLeft() => this.BUS.ULA.PokeKey(Keys.D1);

        public override void PushRight() => this.BUS.ULA.PokeKey(Keys.D2);

        public override void PushFire() => this.BUS.ULA.PokeKey(Keys.D5);

        public override void ReleaseUp() => this.BUS.ULA.PullKey(Keys.D4);

        public override void ReleaseDown() => this.BUS.ULA.PullKey(Keys.D3);

        public override void ReleaseLeft() => this.BUS.ULA.PullKey(Keys.D1);

        public override void ReleaseRight() => this.BUS.ULA.PullKey(Keys.D2);

        public override void ReleaseFire() => this.BUS.ULA.PullKey(Keys.D5);
    }
}
