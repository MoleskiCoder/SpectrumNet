namespace SpectrumNet
{
    internal sealed class KempstonJoystick : Joystick
    {
        private enum Switch
        {
            Right = 0b00000001,
            Left = 0b00000010,
            Down = 0b00000100,
            Up = 0b00001000,
            Fire = 0b00010000,
        }

        private byte contents;

        public KempstonJoystick(Board motherboard)
        : base(motherboard) => this.BUS.Ports.ReadingPort += this.Ports_ReadingPort;

        public override void PushUp() => this.Set(Switch.Up);

        public override void PushDown() => this.Set(Switch.Down);

        public override void PushLeft() => this.Set(Switch.Left);

        public override void PushRight() => this.Set(Switch.Right);

        public override void PushFire() => this.Set(Switch.Fire);

        public override void ReleaseUp() => this.Reset(Switch.Up);

        public override void ReleaseDown() => this.Reset(Switch.Down);

        public override void ReleaseLeft() => this.Reset(Switch.Left);

        public override void ReleaseRight() => this.Reset(Switch.Right);

        public override void ReleaseFire() => this.Reset(Switch.Fire);

        private void Ports_ReadingPort(object? sender, EightBit.PortEventArgs e)
        {
            if (e.Port.Low == 0x1f)
            {
                this.BUS.Ports.WriteInputPort(e.Port, this.contents);
            }
        }

        private void Set(Switch which) => this.contents = EightBit.Chip.SetBit(this.contents, (byte)which);

        private void Reset(Switch which) => this.contents = EightBit.Chip.ClearBit(this.contents, (byte)which);
    }
}
