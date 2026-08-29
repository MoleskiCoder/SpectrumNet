namespace SpectrumNet.UnitTests
{

[TestClass]
    public sealed class AbstractUlaTests
    {
        private readonly Configuration _configuration = new();
        private readonly SealedBuzzer _buzzer = new(44100);
        private readonly SealedColorPalette _palette = new();
        private readonly SealedBoard _board;
        //private readonly SealedUla _ula;

        public AbstractUlaTests()
        {
            this._board = new SealedBoard();
            //this._ula = new SealedUla(this._board);
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
        public void TestIntDuration()
        {
            //Assert.IsTrue(this._ula.Powered);
            Assert.IsTrue(this._board.ULA.Powered);
        }
    }
}
