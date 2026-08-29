namespace SpectrumNet.UnitTests
{
    internal class SealedUla : AbstractUla<uint, byte>
    {
        private readonly AbstractColorPalette<uint> _palette = new SealedColorPalette();

        internal SealedUla(SealedBoard board)
        : base(board, board.CPU, board.Ports, board.VRAM, board.Sound)
        {
        }

        protected override AbstractColorPalette<uint> Palette => _palette;

        protected override void InitialiseKeyboardMapping()
        {
            // Minimal implementation to satisfy the abstract contract.
            // Populate _keyboardMapping here if your code expects keyboard behaviour.
        }
    }
}