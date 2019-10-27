namespace SpectrumNet
{
    using System.Collections.Generic;

    public sealed class Board  : EightBit.Bus
    {
        private readonly Configuration configuration;
        private readonly ColourPalette palette;
        private readonly List<Expansion> expansions = new List<Expansion>();

        private int allowed = 0;

        public Board(ColourPalette palette, Configuration configuration)
        {
            this.palette = palette;
            this.configuration = configuration;
            this.CPU = new EightBit.Z80(this, this.Ports);
            this.ULA = new Ula(this.palette, this);
        }

        public EightBit.Z80 CPU { get; }

        public Ula ULA { get; }

        public Buzzer Sound { get; } = new Buzzer();

        public EightBit.InputOutput Ports { get; } = new EightBit.InputOutput();

        public EightBit.Rom ROM { get; } = new EightBit.Rom();

        public EightBit.Ram VRAM { get; } = new EightBit.Ram(0x4000);

        public EightBit.Ram WRAM { get; } = new EightBit.Ram(0x8000);

        public int NumberOfExpansions => this.expansions.Count;

        public override void Initialize()
        {
            var romDirectory = this.configuration.RomDirectory;
            this.Plug(romDirectory + "\\48.rom");	// ZX Spectrum Basic

            this.ULA.Proceed += this.ULA_Proceed;
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();

            foreach (var expansion in this.expansions)
            {
                expansion.RaisePOWER();
            }

            this.ULA.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.LowerRESET();
            this.CPU.RaiseHALT();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();
        }

        public override void LowerPOWER()
        {
            this.CPU.LowerPOWER();
            this.ULA.LowerPOWER();

            foreach (var expansion in this.expansions)
            {
                expansion.LowerPOWER();
            }

            base.LowerPOWER();
        }

        public void Plug(Expansion expansion) => this.expansions.Add(expansion);

        public Expansion Expansion(int i) => this.expansions[i];

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

        public void RunVerticalBlank()
        {
            for (var i = 0; i < Ula.VerticalRetraceLines; ++i)
            {
                this.ULA.RenderLine(i);
            }
        }

        public void RunRasterLines()
        {
            for (var i = 0; i < Ula.RasterHeight; ++i)
            {
                this.ULA.RenderLine(i + Ula.VerticalRetraceLines);
            }
        }

        public override EightBit.MemoryMapping Mapping(ushort absolute)
        {
            if (absolute < 0x4000)
            {
                return new EightBit.MemoryMapping(this.ROM, 0x0000, 0xffff, EightBit.AccessLevel.ReadOnly);
            }

            if (absolute < 0x8000)
            {
                return new EightBit.MemoryMapping(this.VRAM, 0x4000, 0xffff, EightBit.AccessLevel.ReadWrite);
            }

            return new EightBit.MemoryMapping(this.WRAM, 0x8000, 0xffff, EightBit.AccessLevel.ReadWrite);
        }

        private void RunCycles(int suggested)
        {
            this.allowed += suggested;
            var taken = this.CPU.Run(this.allowed);
            this.allowed -= taken;
        }

        private void ULA_Proceed(object sender, SteppingEventArgs e) => this.RunCycles(e.Cycles);
    }
}
