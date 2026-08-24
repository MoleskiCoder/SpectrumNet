using SpectrumNet;

namespace SpectrumNet
{
    internal sealed class SnaFile(string path) : LittleEndianContent
    {
        private readonly string _path = path;
        private byte _border = 0xff;

        private void LoadRegisters(Z80.Z80 cpu)
        {
            this.ResetPosition();

            cpu.RaiseRESET();

            cpu.IV = this.FetchByte();

            // Alternate set first
            cpu.HL.Assign(this.FetchShort());
            cpu.DE.Assign(this.FetchShort());
            cpu.BC.Assign(this.FetchShort());
            cpu.AF.Assign(this.FetchShort());

            cpu.Exx();

            // Current set
            cpu.HL.Assign(this.FetchShort());
            cpu.DE.Assign(this.FetchShort());
            cpu.BC.Assign(this.FetchShort());

            cpu.IY.Assign(this.FetchShort());
            cpu.IX.Assign(this.FetchShort());

            cpu.IFF2 = (this.FetchByte() >> 2) != 0;
            cpu.REFRESH = this.FetchByte();

            cpu.ExxAF();

            cpu.AF.Assign(this.FetchShort()); // Current
            cpu.SP.Assign(this.FetchShort());
            cpu.IM = this.FetchByte();

            this._border = this.FetchByte();
        }

        private void LoadMemory(Board board)
        {
            var destination = (ushort)board.ROM.Size;
            while (!this.Finished)
                board.Poke(destination++, this.FetchByte());
        }

        public void Load(Board board)
        {
            base.Load(this._path);

            // N.B. Power must be raised prior to loading
            // registers, otherwise power on defaults will override
            // loaded values.
            if (!board.CPU.Powered)
                throw new InvalidOperationException("Whoops: CPU has not been powered on.");

            this.LoadRegisters(board.CPU);
            this.LoadMemory(board);

            board.ULA.SetBorder(this._border);

            // XXXX HACK, HACK, HACK!!
            var original = board.CPU.PeekShort(0xfffe);
            board.Poke(0xfffe, 0xed);
            board.Poke(0xffff, 0x45);   // ED45 is RETN
            board.CPU.PC.Joined = 0xfffe;
            board.CPU.Step();
            board.CPU.PokeShort(0xfffe, original);
        }
    }
}
