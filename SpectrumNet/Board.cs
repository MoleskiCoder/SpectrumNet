namespace SpectrumNet
{
    using SDL3;
    using System.Diagnostics;

    internal sealed class Board : AbstractBoard
    {
        // 48K ROM LD-BYTES entry point, trapped for instant tape loading
        private const ushort LdBytesAddress = 0x0556;

        private TapeFile? _tape;

        public Board(Configuration configuration)
        : base(configuration)
        {
            this.Sound = new Buzzer(this.Timings);
            this.ULA = new Ula(this);
        }

        public AbstractUla<uint, SDL.Keycode> ULA { get; }

        public AbstractBuzzer Sound { get; }

        public override void Initialize()
        {
            base.Initialize();
            var romDirectory = this.Settings.RomDirectory;
            this.Plug(romDirectory + "\\48.rom");	// ZX Spectrum Basic
            this.ULA.Proceed += this.ULA_Proceed;
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.Sound.RaisePOWER();
            this.ULA.RaisePOWER();
        }

        public override void LowerPOWER()
        {
            this.ULA.LowerPOWER();
            this.Sound.LowerPOWER();
            base.LowerPOWER();
        }

        public void LoadSna(string path)
        {
            var sna = new SnaFile(path);
            sna.Load(this);
        }

        public void LoadZ80(string path)
        {
            var z80 = new Z80File(path);
            z80.Load(this);
        }

        public void InsertTape(string path)
        {
            var tape = TapeFile.Create(path);
            tape.Read();
            if (this._tape is null)
            {
                this.CPU.ExecutingInstruction += this.CPU_CheckTapeTrap;
            }

            this._tape = tape;
        }

        public void EjectTape()
        {
            if (this._tape is not null)
            {
                this.CPU.ExecutingInstruction -= this.CPU_CheckTapeTrap;
            }

            this._tape = null;
        }

        public void RenderLines() => this.ULA.RenderLines();

        private void ULA_Proceed(object? sender, EventArgs e) => this.RunCycle();

        private void CPU_CheckTapeTrap(object? sender, EventArgs e)
        {
            if (this.CPU.PC.Joined == LdBytesAddress)
            {
                this.LoadTapeBlock();
            }
        }

        private void LoadTapeBlock()
        {
            Debug.Assert(this._tape is not null, "No tape has been inserted.");

            // LD-BYTES entry conditions:
            // * A = expected flag byte, carry set for LOAD (reset for VERIFY)
            // * IX = destination
            // * DE = length.

            var success = false;
            if (this._tape.TryNextBlock(out var block) && (block.Length >= 2) && (block[0] == this.CPU.A))
            {
                var available = block.Length - 2;   // Exclude the flag and checksum bytes
                var requested = this.CPU.DE.Joined;
                success = available >= requested;
                var loading = this.CPU.Carry() != 0;
                if (success && loading)
                {
                    for (var i = 0; i < requested; ++i)
                    {
                        this.Poke(this.CPU.IX.Joined++, block[1 + i]);
                    }
                }
            }

            if (success)
                this.CPU.SetBit(Z80.StatusBits.CF);
            else
                this.CPU.ClearBit(Z80.StatusBits.CF);

            // Emulate LD-BYTES' RET back to its caller
            this.CPU.Return();
        }
    }
}
