namespace SpectrumNet
{
    using System;
    using System.Diagnostics;

    internal sealed class Z80File(string path) : LittleEndianContent
    {
        private enum HardwareMode
        {
            FortyEightK,
            FortyEightK_IF1,
            SamRam,
            OneTwentyEightK,
            OneTwentyEightK_IF1,
            Unknown = -1
        }

        private readonly string _path = path;

        private int _version;   // Illegal, by default!

        private byte _misc1;
        private byte _misc2;

        private readonly EightBit.Register16 _additionalHeaderLength = new();

        private byte _hardwareMode;
        private byte _emulationMode;

        private readonly byte?[] _window =
        {
            null,
            null,
            null,
            null
        };

        private readonly ushort?[] _block_addresses_48k =
        {
            0,				// 0	(48K ROM)
		    null,	        // 1	(Interface I, Disciple or Plus D ROM)
		    null,	        // 2
		    null,	        // 3
		    0x8000,			// 4
		    0xc000,			// 5
		    null,	        // 6
		    null,	        // 7
		    0x4000,			// 8
		    null,	        // 9
		    null,	        // 10
		    null,	        // 11	(Multiface ROM)
	    };

        private int RefreshHigh => this._misc1 & (byte)EightBit.Mask.One;

        private int Border => (this._misc1 >> 1) & (int)EightBit.Mask.Three;
        private int IM => this._misc2 & (byte)EightBit.Mask.Two;
        private bool Compressed => (this._misc1 & (byte)EightBit.Bits.Bit5) != 0;   // Only valid for V1

        public void Load(Board board)
        {
            base.Load(this._path);

            // N.B. Power must be raised prior to loading
            // registers, otherwise power on defaults will override
            // loaded values.
            if (!board.CPU.Powered)
                throw new InvalidOperationException("CPU has not been powered on.");

            this.LoadRegisters(board.CPU);
            this.LoadMemory(board);

            board.ULA.SetBorder(this.Border);
        }

        private void LoadRegisters(Z80.Z80 cpu)
        {
            Debug.Assert(cpu is not null);

            this.ResetPosition();

            cpu.RaiseRESET();

            // V1

            cpu.A = this.FetchByte();
            cpu.F = this.FetchByte();

            cpu.BC.Assign(this.FetchShort());
            cpu.HL.Assign(this.FetchShort());
            cpu.PC.Assign(this.FetchShort());
            this._version = cpu.PC.Joined == 0 ? 2 : 1;

            cpu.SP.Assign(this.FetchShort());

            cpu.IV = this.FetchByte();

            cpu.REFRESH = this.FetchByte();
            this._misc1 = this.FetchByte();
            this._misc1 = this._misc1 == 0xff ? (byte)1 : this._misc1;
            cpu.REFRESH &= (byte)(this.RefreshHigh << 7);

            cpu.DE.Assign(this.FetchShort());

            cpu.Exx();

            cpu.BC.Assign(this.FetchShort());
            cpu.DE.Assign(this.FetchShort());
            cpu.HL.Assign(this.FetchShort());

            cpu.ExxAF();

            cpu.A = this.FetchByte();
            cpu.F = this.FetchByte();

            cpu.IY.Assign(this.FetchShort());
            cpu.IX.Assign(this.FetchShort());

            cpu.IFF1 = this.FetchByte() != 0;
            cpu.IFF2 = this.FetchByte() != 0;

            this._misc2 = this.FetchByte();
            cpu.IM = this.IM;

            cpu.Exx();
            cpu.ExxAF();

            if (this._version == 1) return;
            Debug.Assert(this._version > 1);

            this._additionalHeaderLength.Assign(this.FetchShort());
            this._version = this._additionalHeaderLength.Joined == 23 ? 2 : 3;

            cpu.PC.Assign(this.FetchShort());

            this._hardwareMode = this.FetchByte();
            if (this._hardwareMode > (byte)HardwareMode.FortyEightK_IF1)
                throw new InvalidDataException("Only 48K ZX Spectrum (with or without Interface I) is supported");

            var state_35 = this.FetchByte(); // offset 35
            var state_36 = this.FetchByte(); // offset 36

            this._emulationMode = this.FetchByte(); // offset 37

            var last_soundchip_register_number = this.FetchByte(); // offset 38, soundchip register number
            byte[] soundchip_registers = new byte[16];
            for (int i = 0; i < 16; ++i)
                soundchip_registers[i] = this.FetchByte(); // offset 39 - 54, sound chip registers

            Debug.Assert(this._version == 2);
        }

        private void LoadMemory(Board board)
        {
            switch (this._version)
            {
                case 1:
                    this.LoadMemoryV1(board);
                    break;
                case 2:
                    this.LoadMemoryV2(board);
                    break;
                default:
                    throw new InvalidDataException($"Only V1 or V2 Z80 files are handled ({this._version} is unsupported).");
            }
        }

        private void ResetWindow()
        {
            this._window[0] = null;
            this._window[1] = null;
            this._window[2] = null;
            this._window[3] = null;
        }

        private bool CompressedWindow =>
            (this._window[0] == 0xed) && (this._window[1] == 0xed);

        private bool FinishedWindow =>
            (this._window[0] == 0x00) && (this._window[1] == 0xed) && (this._window[2] == 0xed) && (this._window[3] == 0x00);

        private void AdjustWindow(byte? current)
        {
            for (int i = 2; i >= 0; --i)
                this._window[i + 1] = this._window[i];
            this._window[0] = current;
        }

        private byte FetchByteWindowed()
        {
            var current = FetchByte();
            this.AdjustWindow(current);
            return current;
        }

        private void LoadMemoryCompressedV1(Board board)
        {
            this.ResetWindow();
            var destination = (ushort)board.ROM.Size;
            while (true)
            {
                var current = this.FetchByteWindowed();
                if (this.CompressedWindow)
                {
                    var repeats = this.FetchByteWindowed();
                    if (this.FinishedWindow) break;
                    var value = this.FetchByteWindowed();
                    --destination;  // Overwrite the initial ED of the compressed marker
                    for (int j = 0; j < repeats; ++j)
                        board.Poke(destination++, value);
                }
                else
                {
                    board.Poke(destination++, current);
                }
            }
        }

        private void LoadMemoryUncompressed(Board board)
        {
            var destination = (ushort)board.ROM.Size;
            while (!this.Finished)
                board.Poke(destination++, this.FetchByte());
        }

        private void LoadMemoryV1(Board board)
        {
            if (this.Compressed)
                this.LoadMemoryCompressedV1(board);
            else
                this.LoadMemoryUncompressed(board);
        }

        private void LoadMemoryCompressedV2(Board board)
        {
            Debug.Assert((this._hardwareMode == (byte)HardwareMode.FortyEightK) || (this._hardwareMode == (byte)HardwareMode.FortyEightK_IF1));
            var length = this.FetchShort();
            var page = this.FetchByte();
            this.ResetWindow();
            var destination = this._block_addresses_48k[page] ?? throw new InvalidDataException($"Invalid block address page ({page}.");
            var remaining = length;
            while (remaining.Joined > 0)
            {
                var current = this.FetchByteWindowed();
                remaining.Decrement();
                if (this.CompressedWindow)
                {
                    var repeats = this.FetchByteWindowed();
                    remaining.Decrement();
                    var value = this.FetchByteWindowed();
                    remaining.Decrement();
                    --destination;  // Overwrite the initial ED of the compressed marker
                    for (int j = 0; j < repeats; ++j)
                        board.Poke(destination++, value);
                }
                else
                {
                    board.Poke(destination++, current);
                }
            }
        }

        private void LoadMemoryV2(Board board)
        {
            while (!this.Finished)
                this.LoadMemoryCompressedV2(board);
        }
    }
}
