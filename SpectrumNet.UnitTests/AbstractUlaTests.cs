namespace SpectrumNet.UnitTests
{

    [TestClass]
    public sealed class AbstractUlaTests
    {
        private readonly SealedBoard _board;
        private SealedUla ULA => this._board.ULA as SealedUla ?? throw new InvalidOperationException("ULA is not a SealedUla.");
        private Z80.Z80 CPU => this._board.CPU;

        private bool _finished;
        private long _ticks;

        public AbstractUlaTests()
        {
            this._board = new SealedBoard();
        }

        [TestInitialize]
        public void Setup()
        {
            this._board.RaisePOWER();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this._board.LowerPOWER();
        }

        [TestMethod]
        public void TestUlaPowersUp()
        {
            Assert.IsTrue(this.ULA.Powered);
        }

        [TestMethod]
        public void TestInterruptDuration()
        {
            this.CPU.LoweringINT += CPU_LoweringINT;
            this.CPU.RaisingINT += CPU_RaisingINT;
            this.ULA.Ticked += ULA_Ticked;

            this._finished = true;
            this._ticks = 0;
            this.ULA.RenderLines();

            Assert.AreEqual(AbstractUla<uint, byte>.InterruptDuration, this._ticks);

            this.ULA.Ticked -= ULA_Ticked;
            this.CPU.RaisingINT -= CPU_RaisingINT;
            this.CPU.LoweringINT -= CPU_LoweringINT;
        }

        private void ULA_Ticked(object? sender, EventArgs e)
        {
            if (!this._finished)
                ++this._ticks;
        }

        private void CPU_LoweringINT(object? sender, EventArgs e)
        {
            this._finished = false;
        }

        private void CPU_RaisingINT(object? sender, EventArgs e)
        {
            this._finished = true;
        }

        [TestMethod]
        public void TestInterruptInterval()
        {
            this.ULA.Ticked += ULA_Ticked;

            this._finished = false;
            this._ticks = 0;
            this.ULA.RenderLines();

            Assert.AreEqual(69888 * 2, this._ticks);

            this.ULA.Ticked -= ULA_Ticked;
        }
    }
}
