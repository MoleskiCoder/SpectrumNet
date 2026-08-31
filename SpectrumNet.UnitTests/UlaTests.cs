namespace SpectrumNet.UnitTests
{

    [TestClass]
    public sealed class UlaTests
    {
        private readonly SealedBoard _board;
        private SealedUla ULA => this._board.ULA as SealedUla ?? throw new InvalidOperationException("ULA is not a SealedUla.");
        private Z80.Z80 CPU => this._board.CPU;

        private bool _frameFinished;
        private long _frameTicks;
        private long _interruptCount;
        private bool _interruptFinished;
        private long _interruptTicks;

        public UlaTests()
        {
            var timings = new PalTimings();
            this._board = new SealedBoard(timings);
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
        public void TestSingleFrame()
        {
            this.CPU.LoweringINT += this.CPU_LoweringINT;
            this.CPU.RaisingINT += this.CPU_RaisingINT;
            this.ULA.Ticked += this.ULA_Ticked;

            this._frameFinished = false;
            this._interruptFinished = true;
            this._frameTicks = this._interruptTicks = 0;

            var startingF = this.ULA.F;

            this.ULA.RenderLines();

            Assert.AreEqual(AbstractUla<uint, byte>.InterruptDuration, this._interruptTicks);
            Assert.AreEqual(this.ULA.TotalHorizontalClocks * this.ULA.TotalHeight, this._frameTicks);
            Assert.AreEqual(1, this._interruptCount);
            Assert.AreEqual(startingF + 1, this.ULA.F);

            this.ULA.Ticked -= this.ULA_Ticked;
            this.CPU.RaisingINT -= this.CPU_RaisingINT;
            this.CPU.LoweringINT -= this.CPU_LoweringINT;
        }

        [TestMethod]
        public void TestActiveScanLine()
        {
            this.ULA.Ticked += this.ULA_Ticked;
            this._frameTicks = 0;
            this.ULA.RenderLine();
            Assert.AreEqual(this.ULA.TotalHorizontalClocks, this._frameTicks);
            this.ULA.Ticked -= this.ULA_Ticked;
        }

        [TestMethod]
        public void TestInterruptToInterrupt()
        {
            this.CPU.LoweringINT += this.CPU_LoweringINT;

            this.ULA.RenderLines();
            this.ULA.RenderLines();

            Assert.AreEqual(2, this._interruptCount);

            this.CPU.RaisingINT -= this.CPU_RaisingINT;
        }

        private void ULA_Ticked(object? sender, EventArgs e)
        {
            ++this._frameTicks;
            if (!this._interruptFinished)
                ++this._interruptTicks;
        }

        private void CPU_LoweringINT(object? sender, EventArgs e)
        {
            Assert.AreEqual(0, this.ULA.V);
            Assert.AreEqual(0, this.ULA.C);
            this._interruptFinished = false;
            ++this._interruptCount;
        }

        private void CPU_RaisingINT(object? sender, EventArgs e)
        {
            this._interruptFinished = true;
        }
    }
}
