namespace SpectrumNet
{
    internal sealed class SnaFile(string path, Board bus) : SnapshotFile(path, bus)
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

        public override void Load()
        {
            base.Load();

            this.BUS.ULA.UpdateBorder(this.Peek(Offset_BorderColour));

            // XXXX HACK, HACK, HACK!!
            var original = this.CPU.PeekShort(0xfffe);
            this.BUS.Poke(0xfffe, 0xed);
            this.BUS.Poke(0xffff, 0x45);   // ED45 is RETN
            this.CPU.PC.Joined = 0xfffe;
            _ = this.CPU.Step();
            this.CPU.PokeShort(0xfffe, original);
        }

        protected override void LoadRegisters()
        {
            this.CPU.RaiseRESET();

            this.CPU.IV = this.Peek(Offset_I);

            this.CPU.HL.Assign(this.PeekShort(Offset_HL_));
            this.CPU.DE.Assign(this.PeekShort(Offset_DE_));
            this.CPU.BC.Assign(this.PeekShort(Offset_BC_));
            this.CPU.AF.Assign(this.PeekShort(Offset_AF_));

            this.CPU.Exx();

            this.CPU.HL.Assign(this.PeekShort(Offset_HL));
            this.CPU.DE.Assign(this.PeekShort(Offset_DE));
            this.CPU.BC.Assign(this.PeekShort(Offset_BC));

            this.CPU.IY.Assign(this.PeekShort(Offset_IY));
            this.CPU.IX.Assign(this.PeekShort(Offset_IX));
            this.CPU.IFF2 = (this.Peek(Offset_IFF2) >> 2) != 0;
            this.CPU.REFRESH = this.Peek(Offset_R);

            this.CPU.ExxAF();

            this.CPU.AF.Assign(this.PeekShort(Offset_AF));
            this.CPU.SP.Assign(this.PeekShort(Offset_SP));
            this.CPU.IM = this.Peek(Offset_IM);
        }

        protected override void LoadMemory()
        {
            for (var i = 0; i < RamSize; ++i)
            {
                this.BUS.Poke((ushort)(this.BUS.ROM.Size + i), this.Peek((ushort)(HeaderSize + i)));
            }
        }
    }
}
