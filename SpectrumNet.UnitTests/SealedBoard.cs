namespace SpectrumNet.UnitTests
{
    using SDL3;

    internal sealed class SealedBoard : AbstractBoard
    {
        private readonly SealedUla _ula;

        public SealedBoard()
        : base(false)
        {
            this._ula = new SealedUla(this);
        }

        public AbstractUla<uint, byte> ULA => this._ula;

        public AbstractBuzzer Sound { get; } = new SealedBuzzer(44100);

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.Sound.RaisePOWER();
            this._ula.RaisePOWER();
        }

        public override void LowerPOWER()
        {
            this._ula.LowerPOWER();
            this.Sound.LowerPOWER();
            base.LowerPOWER();
        }
    }
}
