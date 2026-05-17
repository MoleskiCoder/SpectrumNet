namespace SpectrumNet
{
    internal class SnaFile(string path) : SnapshotFile(path)
    {
        private const int Offset_I = 0x0;
        private const int Offset_HL_ = 0x1;
        private const int Offset_DE_ = 0x3;
        private const int Offset_BC_ = 0x5;
        private const int Offset_AF_ = 0x7;
        private const int Offset_HL = 0x9;
        private const int Offset_DE = 0xb;
        private const int Offset_BC = 0xd;
        private const int Offset_IY = 0xf;
        private const int Offset_IX = 0x11;
        private const int Offset_IFF2 = 0x13;
        private const int Offset_R = 0x14;
        private const int Offset_AF = 0x15;
        private const int Offset_SP = 0x17;
        private const int Offset_IM = 0x19;
        private const int Offset_BorderColour = 0x1a;

        private const int HeaderSize = Offset_BorderColour + 1;

        private const int RamSize = (32 + 16) * 1024;

        public override void Load(Board board)
        {
            base.Load(board);

            board.ULA.UpdateBorder(this.Peek(Offset_BorderColour));

            // XXXX HACK, HACK, HACK!!
            var original = board.CPU.PeekShort(0xfffe);
            board.Poke(0xfffe, 0xed);
            board.Poke(0xffff, 0x45);   // ED45 is RETN
            board.CPU.PC.Joined = 0xfffe;
            _ = board.CPU.Step();
            board.CPU.PokeShort(0xfffe, original);
        }

        protected override void LoadRegisters(Z80.Z80 cpu)
        {
            cpu.RaiseRESET();

            cpu.IV = this.Peek(Offset_I);

            cpu.HL.Joined = this.PeekShort(Offset_HL_);
            cpu.DE.Joined = this.PeekShort(Offset_DE_);
            cpu.BC.Joined = this.PeekShort(Offset_BC_);
            cpu.AF.Joined = this.PeekShort(Offset_AF_);

            cpu.Exx();

            cpu.HL.Joined = this.PeekShort(Offset_HL);
            cpu.DE.Joined = this.PeekShort(Offset_DE);
            cpu.BC.Joined = this.PeekShort(Offset_BC);

            cpu.IY.Joined = this.PeekShort(Offset_IY);
            cpu.IX.Joined = this.PeekShort(Offset_IX);
            cpu.IFF2 = (this.Peek(Offset_IFF2) >> 2) != 0;
            cpu.REFRESH = this.Peek(Offset_R);

            cpu.ExxAF();

            cpu.AF.Joined = this.PeekShort(Offset_AF);
            cpu.SP.Joined = this.PeekShort(Offset_SP);
            cpu.IM = this.Peek(Offset_IM);
        }

        protected override void LoadMemory(Board board)
        {
            for (var i = 0; i < RamSize; ++i)
            {
                board.Poke((ushort)(board.ROM.Size + i), this.Peek((ushort)(HeaderSize + i)));
            }
        }
    }
}
