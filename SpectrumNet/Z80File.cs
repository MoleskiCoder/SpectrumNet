namespace SpectrumNet
{
    using System;

    public sealed class Z80File : SnapshotFile
    {
        // V1 Header block

        private const int Offset_A = 0;
        private const int Offset_F = 1;
        private const int Offset_BC = 2;
        private const int Offset_HL = 4;
        private const int Offset_PC = 6;
        private const int Offset_SP = 8;
        private const int Offset_I = 10;
        private const int Offset_R = 11; // Bit 7 is not significant!

        // Bit 0	 : Bit 7 of the R - register
        // Bit 1 - 3 : Border colour
        // Bit 4     : 1 = Basic SamRom switched in
        // Bit 5     : 1 = Block of data is compressed
        // Bit 6 - 7 : No meaning
        private const int Offset_misc_1 = 12;

        private const int Offset_DE = 13;
        private const int Offset_BC_ = 15;
        private const int Offset_DE_ = 17;
        private const int Offset_HL_ = 19;
        private const int Offset_A_ = 21;
        private const int Offset_F_ = 22;
        private const int Offset_IY = 23;
        private const int Offset_IX = 25;
        private const int Offset_IFF1 = 27;
        private const int Offset_IFF2 = 28;

        // Bit 0 - 1 : Interrupt mode(0, 1 or 2)
        // Bit 2     : 1 = Issue 2 emulation
        // Bit 3     : 1 = Double interrupt frequency
        // Bit 4 - 5 : 1 = High video synchronisation
        //             3 = Low video synchronisation
        //             0, 2 = Normal
        // Bit 6 - 7 : 0 = Cursor / Protek / AGF joystick
        //             1 = Kempston joystick
        //             2 = Sinclair 2 Left joystick(or user
        //	               defined, for version 3.z80 files)
        //             3 = Sinclair 2 Right joystick
        private const int Offset_misc_2 = 29;

        private const int HeaderSizeV1 = Offset_misc_2 + 1;

        private const int RamSize = (32 + 16) * 1024;

        private int version = 0;    // Illegal, by default!

        protected override void ExamineHeaders()
        {
            switch (this.PeekWord(Offset_PC))
            {
                case 0:
                    this.version = 2;
                    break;
                default:
                    this.version = 1;
                    break;
            }
        }

        public Z80File(string path)
        : base(path)
        {
        }

	    public override void Load(Board board)
        {
            base.Load(board);
            board.ULA.Border = (this.Misc1() >> 1) & (int)EightBit.Mask.Mask3;
        }

        protected override void LoadRegisters(EightBit.Z80 cpu)
        {
            if (this.version != 1)
            {
                throw new InvalidOperationException("Only V1 Z80 supported at the moment");
            }

            cpu.RaiseRESET();

            cpu.A = this.Peek(Offset_A);
            cpu.F = this.Peek(Offset_F);

            cpu.BC.Word = this.PeekWord(Offset_BC);
            cpu.HL.Word = this.PeekWord(Offset_HL);
            cpu.PC.Word = this.PeekWord(Offset_PC);
            cpu.SP.Word = this.PeekWord(Offset_SP);

            cpu.IV = this.Peek(Offset_I);

            cpu.REFRESH = this.Peek(Offset_R);
            cpu.REFRESH &= (byte)((this.Misc1() & (byte)EightBit.Mask.Mask1) << 7);

            cpu.DE.Word = this.PeekWord(Offset_DE);

            cpu.Exx();

            cpu.BC.Word = this.PeekWord(Offset_BC_);
            cpu.DE.Word = this.PeekWord(Offset_DE_);
            cpu.HL.Word = this.PeekWord(Offset_HL_);

            cpu.ExxAF();

            cpu.A = this.Peek(Offset_A_);
            cpu.F = this.Peek(Offset_F_);

            cpu.IY.Word = this.PeekWord(Offset_IY);
            cpu.IX.Word = this.PeekWord(Offset_IX);

            cpu.IFF1 = this.Peek(Offset_IFF1) != 0;
            cpu.IFF2 = this.Peek(Offset_IFF2) != 0;

            var misc2 = this.Peek(Offset_misc_2);
            cpu.IM = misc2 & (byte)EightBit.Mask.Mask2;

            cpu.Exx();
            cpu.ExxAF();
        }

        protected override void LoadMemory(Board board)
        {
            switch (this.version)
            {
                case 1:
                    this.LoadMemoryV1(board);
                    break;
                default:
                    throw new InvalidOperationException("Only V1 Z80 files are handled.");
            }
        }

        private byte Misc1()
        {
            var misc1 = this.Peek(Offset_misc_1);
            return misc1 == 0xff ? (byte)1 : (byte)misc1;
        }

        private void LoadMemoryV1(Board board)
        {
            var compressed = (this.Misc1() & (byte)EightBit.Bits.Bit5) != 0;
            if (compressed)
            {
                this.LoadMemoryCompressedV1(board, HeaderSizeV1);
            }
            else
            {
                this.LoadMemoryUncompressed(board, HeaderSizeV1);
            }
        }

        private void LoadMemoryCompressedV1(Board board, ushort offset)
        {
            var position = board.ROM.Size;
            var fileSize = this.Size - 4;
            this.LoadCompressedBlock(board, offset, (ushort)position, (ushort)fileSize);
        }

        private void LoadMemoryUncompressed(Board board, int offset)
        {
            for (var i = 0; i < RamSize; ++i)
            {
                board.Poke((ushort)(board.ROM.Size + i), this.Peek((ushort)(offset + i)));
            }
        }

        private ushort LoadCompressedBlock(Board board, ushort source)
        {
            var length = this.PeekWord(source);
            var block = this.Peek((ushort)(source + 2));
            this.LoadCompressedBlock(board, (ushort)(source + 3), (ushort)(block * 0x4000), length);
            return length;
        }

        private void LoadCompressedBlock(Board board, ushort source, ushort destination, ushort length)
        {
            var previous = 0x100;
            for (var i = source; i != length; ++i)
            {
                var current = this.ROM.Peek(i);
                if (current == 0xed && previous == 0xed)
                {
                    var repeats = this.Peek(++i);
                    var value = this.Peek(++i);
                    --destination;
                    for (var j = 0; j < repeats; ++j)
                    {
                        board.Poke(destination++, value);
                    }

                    previous = 0x100;
                }
                else
                {
                    board.Poke(destination++, current);
                    previous = current;
                }
            }
        }
    }
}
