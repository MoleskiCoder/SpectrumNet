namespace SpectrumNet.UnitTests
{
    internal sealed class SealedBoard : AbstractBoard
    {
        private readonly SealedUla _ula;
        private readonly SealedBuzzer _sound;

        public SealedBoard(ITimings timings)
        : base(timings, false)
        {
            this._sound = new SealedBuzzer(timings, 44100);
            this._ula = new SealedUla(this);
        }

        public AbstractUla<uint, byte> ULA => this._ula;

        public AbstractBuzzer Sound => this._sound;

        public override void Initialize()
        {
            base.Initialize();
            this.ULA.Proceed += this.ULA_Proceed;
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this._sound.RaisePOWER();
            this._ula.RaisePOWER();
        }

        public override void LowerPOWER()
        {
            this._ula.LowerPOWER();
            this._sound.LowerPOWER();
            base.LowerPOWER();
        }

        private void ULA_Proceed(object? sender, EventArgs e) => this.RunCycle();
    }
}
