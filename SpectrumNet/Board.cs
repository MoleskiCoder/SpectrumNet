using EightBit;
using System.Diagnostics;

namespace SpectrumNet
{
    internal sealed class Board : EightBit.Bus, IDisposable
    {
        // 48K ROM LD-BYTES entry point, trapped for instant tape loading
        private const ushort LdBytesAddress = 0x0556;

        private readonly Configuration _configuration;
        private readonly ColorPalette _palette;
        private readonly List<Expansion> _expansions = [];

        private readonly Z80.Disassembler? _disassembler;

        private TapeFile? _tape;

        private int _allowed;

        private readonly EightBit.MemoryMapping _romMapping;
        private readonly EightBit.MemoryMapping _vramMapping;
        private readonly EightBit.MemoryMapping _wramMapping;

        private bool _disposed;

        public Board(ColorPalette palette, Configuration configuration)
        {
            this._palette = palette;
            this._configuration = configuration;
            this.CPU = new Z80.Z80(this, this.Ports);
            this.ULA = new Ula(this._palette, this);
            if (this._configuration.DebugMode)
            {
                this._disassembler = new Z80.Disassembler(this);
            }

            this._romMapping = new(this.ROM, 0x0000, 0xffff, EightBit.AccessLevel.ReadOnly);
            this._vramMapping = new(this.VRAM, 0x4000, 0xffff, EightBit.AccessLevel.ReadWrite);
            this._wramMapping = new(this.WRAM, 0x8000, 0xffff, EightBit.AccessLevel.ReadWrite);
        }

        public Z80.Z80 CPU { get; }

        public Ula ULA { get; }

        public Buzzer Sound { get; } = new Buzzer();

        public EightBit.InputOutput Ports { get; } = new EightBit.InputOutput();

        public EightBit.Rom ROM { get; } = new EightBit.Rom();

        public EightBit.Ram VRAM { get; } = new EightBit.Ram(0x4000);

        public EightBit.Ram WRAM { get; } = new EightBit.Ram(0x8000);

        public int NumberOfExpansions => this._expansions.Count;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void Initialize()
        {
            var romDirectory = this._configuration.RomDirectory;
            this.Plug(romDirectory + "\\48.rom");	// ZX Spectrum Basic

            this.ULA.Proceed += this.ULA_Proceed;
            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;

            if (this._configuration.DebugMode)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction;
            }
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            foreach (var expansion in this._expansions)
            {
                expansion.RaisePOWER();
            }

            this.ULA.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.LowerRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            this.ULA.LowerPOWER();

            foreach (var expansion in this._expansions)
            {
                expansion.LowerPOWER();
            }

            base.LowerPOWER();
        }

        public void Plug(Expansion expansion) => this._expansions.Add(expansion);

        public Expansion Expansion(int i) => this._expansions[i];

        public void Plug(string path) => this.ROM.Load(path);

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

        public void RenderLines()
        {
            ULA.RenderLines();
        }

        public override EightBit.MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x4000)
            {
                return this._romMapping;
            }

            if (absolute < 0x8000)
            {
                return this._vramMapping;
            }

            return this._wramMapping;
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this.Sound.Dispose();
                }

                this._disposed = true;
            }
        }

        private void RunCycle()
        {
            var taken = this.CPU.Run(++this._allowed);
            this._allowed -= taken;
        }

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

            // LD-BYTES entry conditions: A = expected flag byte, carry set
            // for LOAD (reset for VERIFY), IX = destination, DE = length.
            var requested = this.CPU.DE.Joined;
            var destination = this.CPU.IX.Joined;
            var loading = this.CPU.Carry() != 0;

            var success = false;
            if (this._tape.TryNextBlock(out var block) && (block.Length >= 2) && (block[0] == this.CPU.A))
            {
                var available = block.Length - 2;   // Exclude the flag and checksum bytes
                var count = Math.Min(requested, available);
                if (loading)
                {
                    for (var i = 0; i < count; ++i)
                    {
                        this.Poke((ushort)(destination + i), block[1 + i]);
                    }
                }

                this.CPU.IX.Joined = (ushort)(destination + count);
                this.CPU.DE.Joined = (ushort)(requested - count);
                success = count == requested;
            }

            if (success)
                this.CPU.SetBit(Z80.StatusBits.CF);
            else
                this.CPU.ClearBit(Z80.StatusBits.CF);

            // Emulate LD-BYTES' RET back to its caller
            this.CPU.PC.Joined = this.CPU.PeekShort(this.CPU.SP.Joined).Joined;
            this.CPU.SP.Joined += 2;
        }

        private void CPU_ExecutedInstruction(object? sender, EventArgs e) => this.CPU.RaiseRESET();

        private void CPU_ExecutingInstruction(object? sender, System.EventArgs e)
        {
            Debug.Assert(this._disassembler is not null, "Disassembler has not been initialized.");
            var state = Z80.Disassembler.State(this.CPU);
            var disassembly = this._disassembler.Disassemble(this.CPU);
            System.Console.WriteLine($"{state} {disassembly}");
        }
    }
}
