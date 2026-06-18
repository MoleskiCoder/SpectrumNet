namespace SpectrumNet
{
    using EightBit;
    using System;

    internal abstract class SnapshotFile(string path, Board bus)
    {
        private readonly string _path = path;

        protected EightBit.Rom ROM { get; } = new EightBit.Rom();

        protected Board BUS { get; } = bus;

        protected Z80.Z80 CPU => this.BUS.CPU;

        protected int Size => this.ROM.Size;

        public virtual void Load()
        {
            this.Read();

            // N.B. Power must be raised prior to loading
            // registers, otherwise power on defaults will override
            // loaded values.
            if (!this.CPU.Powered)
            {
                throw new InvalidOperationException("Whoops: CPU has not been powered on.");
            }

            this.ExamineHeaders();
            this.LoadRegisters();
            this.LoadMemory();
        }

        protected virtual void ExamineHeaders()
        {
        }

        protected abstract void LoadRegisters();

        protected abstract void LoadMemory();

        protected void Read() => this.ROM.Load(this._path);

        protected byte Peek(ushort offset) => this.ROM.Peek(offset);

        protected Register16 PeekShort(ushort offset) => this.CPU.PeekShort(offset);
    }
}
