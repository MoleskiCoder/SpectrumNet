namespace SpectrumNet
{
    using System;

    internal abstract class SnapshotFile(string path)
    {
        private readonly string path = path;

        protected EightBit.Rom ROM { get; } = new EightBit.Rom();

        protected int Size => this.ROM.Size;

        public virtual void Load(Board board)
        {
            this.Read();

            // N.B. Power must be raised prior to loading
            // registers, otherwise power on defaults will override
            // loaded values.
            if (!board.CPU.Powered)
            {
                throw new InvalidOperationException("Whoops: CPU has not been powered on.");
            }

            this.ExamineHeaders();
            this.LoadRegisters(board.CPU);
            this.LoadMemory(board);
        }

        protected virtual void ExamineHeaders()
        {
        }

        protected abstract void LoadRegisters(Z80.Z80 cpu);

        protected abstract void LoadMemory(Board board);

        protected void Read() => this.ROM.Load(this.path);

        protected byte Peek(ushort offset) => this.ROM.Peek(offset);

        // Assumed to be little-endian!
        protected ushort PeekWord(ushort offset)
        {
            var low = this.Peek(offset++);
            var high = this.Peek(offset);
            return EightBit.Chip.MakeWord(low, high);
        }
    }
}
