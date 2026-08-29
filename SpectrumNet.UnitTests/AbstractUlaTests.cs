namespace SpectrumNet.UnitTests
{

    [TestClass]
    public sealed class AbstractUlaTests
    {
        private readonly SealedBoard _board;

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
            Assert.IsTrue(this._board.ULA.Powered);
        }
    }
}
