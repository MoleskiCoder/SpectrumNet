namespace SpectrumNet
{
    using System;

    public sealed class Z80File : SnapshotFile
    {
        private enum HardwareModeV2
        {
            FortyEightK,
            FortyEightK_IF1,
            SamRam,
            OneTwentyEightK,
            OneTwentyEightK_IF1,
            Unknown = -1
        }

        const int BlockSize = 0x4000;

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

        private const int Offset_length_additional_header_block = 30;
        private const int Offset_V2_PC = 32;
        private const int Offset_hardware_mode = 34;

        private int version = 0;    // Illegal, by default!
        private HardwareModeV2 hardwareModeV2 = HardwareModeV2.Unknown;

        protected override void ExamineHeaders()
        {
            switch (this.PeekWord(Offset_PC))
            {
                case 0:
                    this.version = this.PeekWord(Offset_length_additional_header_block) == 23 ? 2 : 3;
                    if (this.version == 2)
                    {
                        this.hardwareModeV2 = (HardwareModeV2)this.Peek(Offset_hardware_mode);
                    }

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

        private int HeaderSize
        {
            get
            {
                switch (this.version)
                {
                    case 1:
                        return HeaderSizeV1;
                    case 2:
                        return HeaderSizeV1 + this.PeekWord(Offset_length_additional_header_block) + 2; // Why +2 needed??
                    default:
                        throw new InvalidOperationException("Unknown Z80 file version");
                }
            }
        }

	    public override void Load(Board board)
        {
            base.Load(board);
            board.ULA.UpdateBorder((this.Misc1() >> 1) & (int)EightBit.Mask.Mask3);
        }

        protected override void LoadRegisters(EightBit.Z80 cpu)
        {
            cpu.RaiseRESET();

            cpu.A = this.Peek(Offset_A);
            cpu.F = this.Peek(Offset_F);

            cpu.BC.Word = this.PeekWord(Offset_BC);
            cpu.HL.Word = this.PeekWord(Offset_HL);
            cpu.PC.Word = this.PeekWord(Offset_PC); // Only valid for V1
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

            if (this.version > 1)
            {
                cpu.PC.Word = this.PeekWord(Offset_V2_PC);
            }
        }

        protected override void LoadMemory(Board board)
        {
            switch (this.version)
            {
                case 1:
                    this.LoadMemoryV1(board);
                    break;
                case 2:
                    switch (this.hardwareModeV2)
                    {
                        case HardwareModeV2.FortyEightK:
                        case HardwareModeV2.FortyEightK_IF1:
                            this.LoadMemoryV2(board);
                            break;
                        default:
                            throw new InvalidOperationException("Only 48K ZX Spectrums are handled.");
                    }

                    break;
                default:
                    throw new InvalidOperationException("Only V1 or V2 Z80 files are handled.");
            }
        }

        private byte Misc1()
        {
            var misc1 = this.Peek(Offset_misc_1);
            return misc1 == 0xff ? (byte)1 : (byte)misc1;
        }

        private void LoadMemoryV1(Board board)
        {
            System.Diagnostics.Debug.WriteLine("LoadMemoryV1");

            var compressed = (this.Misc1() & (byte)EightBit.Bits.Bit5) != 0;
            if (compressed)
            {
                this.LoadMemoryCompressedV1(board, (ushort)this.HeaderSize);
            }
            else
            {
                this.LoadMemoryUncompressed(board, (ushort)this.HeaderSize);
            }
        }

        private void LoadMemoryCompressedV1(Board board, ushort offset)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMemoryCompressedV1: offset={offset}");

            var position = board.ROM.Size;
            var fileSize = this.Size - offset - 2;
            this.LoadCompressedBlock(board, offset, (ushort)position, (ushort)fileSize);
        }

        private void LoadMemoryV2(Board board)
        {
            System.Diagnostics.Debug.WriteLine("LoadMemoryV2");

            var position = (ushort)this.HeaderSize;
            while (position < this.Size)
            {
                position += this.LoadMemoryBlock(board, position);
            }
        }

        private ushort LoadMemoryBlock(Board board, ushort offset)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMemoryBlock: offset={offset}");

            var offsetLength = offset;
            var offsetPage = (ushort)(offsetLength + 2);
            var offsetBlock = (ushort)(offsetPage + 1);

            var length = this.PeekWord(offsetLength);
            var page = this.Peek(offsetPage);
            var uncompressed = length == 0xffff;
            if (uncompressed)
            {
                length = 0x4000;
            }

            int convertedPage;
            switch (page)
            {
                case 0: // 48K ROM!
                    throw new InvalidOperationException("Cannot overwrite ROM from Z80 file!");
                case 8: // 0x4000 - 0x7fff
                    convertedPage = 1;
                    break;
                case 4: // 0x8000 - 0xbfff
                    convertedPage = 2;
                    break;
                case 5: // 0xc000 - 0xffff
                    convertedPage = 3;
                    break;
                default:
                    throw new InvalidOperationException("Invalid page load detected!");
            }

            var destination = (ushort)(convertedPage * BlockSize);
            if (uncompressed)
            {
                this.LoadUncompressedBlock(board, offsetBlock, destination, length);
            }
            else
            {
                this.LoadCompressedBlock(board, offsetBlock, destination, length);
            }

            return (ushort)(length + 3);
        }

        private void LoadMemoryUncompressed(Board board, ushort offset)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMemoryUncompressed: offset={offset}");

            var position = offset;
            for (var block = 1; block < 4; ++block)
            {
                this.LoadMemoryUncompressed(board, position, block);
                position += BlockSize;
            }
        }

        private void LoadMemoryUncompressed(Board board, ushort offset, int block)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMemoryUncompressed: offset={offset}, block={block}");

            var start = (ushort)(block * BlockSize);
            this.LoadUncompressedBlock(board, offset, start, BlockSize);
        }

        private void LoadUncompressedBlock(Board board, ushort source, ushort destination, ushort length)
        {
            System.Diagnostics.Debug.WriteLine($"LoadUncompressedBlock: source={source}, destination={destination:x4}, length={length:x4}");

            for (ushort i = 0; i < length; ++i)
            {
                board.Poke(destination++, this.Peek(source++));
            }
        }

        private void LoadCompressedBlock(Board board, ushort source, ushort destination, ushort length)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCompressedBlock: source={source}, destination={destination:x4}, length={length:x4}");

            var previous = 0x100;
            for (var i = source; i < (length + source); ++i)
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
