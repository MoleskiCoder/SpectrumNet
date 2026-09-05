namespace SpectrumNet.UnitTests
{
    using EightBit;
    using Microsoft.Testing.Platform.Configurations;
    using System.Diagnostics;

    [TestClass]
    public sealed class BoardTests
    {
        private readonly Configuration _configuration;
        private SealedBoard? _board;

        private int _instructionsUnderReset;
        private bool _underReset;

        public BoardTests()
        {
            Directory.SetCurrentDirectory(@"c:\github\spectrum");
            this._configuration = new Configuration();
        }

        [TestInitialize]
        public void Setup()
        {
            this._board = new SealedBoard(this._configuration);
            this._board.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction;
            this._board.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;
            this._instructionsUnderReset = 0;
            this._board.Initialize();
            this._board.RaisePOWER();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Debug.Assert(this._board is not null);
            this._board.LowerPOWER();
            this._board.CPU.ExecutingInstruction -= this.CPU_ExecutingInstruction;
            this._board.CPU.ExecutedInstruction -= this.CPU_ExecutedInstruction;
        }

        private void CPU_ExecutingInstruction(object? sender, EventArgs e)
        {
            Debug.Assert(this._board is not null);
            this._underReset = this._board.CPU.RESET.Lowered();
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e)
        {
            Debug.Assert(this._board is not null);
            if (this._underReset)
            {
                Assert.AreEqual(0x00, this._board.CPU.OpCode);
                ++this._instructionsUnderReset;
            }
        }

        [TestMethod]
        public void TestMotherboardPowersUp()
        {
            Debug.Assert(this._board is not null);
            Assert.IsTrue(this._board.Powered);
            var cpu = this._board.CPU;
            Assert.IsTrue(cpu.Powered);
        }

        [TestMethod]
        public void TestPowerOnReset()
        {
            Assert.AreEqual(1, this._instructionsUnderReset);
            this._board?.CPU.PoweredStep();
            Assert.AreEqual(1, this._instructionsUnderReset);
        }
    }
}
