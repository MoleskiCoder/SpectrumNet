namespace SpectrumNet
{
    using System.Diagnostics;

    internal class AbstractBoard : EightBit.Bus
    {
        protected readonly Z80.Disassembler? _disassembler;
        protected readonly bool _disassembling;

        protected readonly EightBit.MemoryMapping _romMapping;
        protected readonly EightBit.MemoryMapping _vramMapping;
        protected readonly EightBit.MemoryMapping _wramMapping;

        protected readonly List<Expansion> _expansions = [];

        private int _allowed;

        public int NumberOfExpansions => this._expansions.Count;

        protected AbstractBoard(ITimings timings, bool disassembling)
        {
            this.Timings = timings;

            this.CPU = new Z80.Z80(this, this.Ports);
            this._disassembler = new Z80.Disassembler(this);
            this._disassembling = disassembling;

            this._romMapping = new(this.ROM, 0x0000, 0xffff, EightBit.AccessLevel.ReadOnly);
            this._vramMapping = new(this.VRAM, 0x4000, 0xffff, EightBit.AccessLevel.ReadWrite);
            this._wramMapping = new(this.WRAM, 0x8000, 0xffff, EightBit.AccessLevel.ReadWrite);
        }

        public ITimings Timings { get; }

        public Z80.Z80 CPU { get; }

        public EightBit.InputOutput Ports { get; } = new EightBit.InputOutput();

        public EightBit.Rom ROM { get; } = new EightBit.Rom();

        public EightBit.Ram VRAM { get; } = new EightBit.Ram(0x4000);

        public EightBit.Ram WRAM { get; } = new EightBit.Ram(0x8000);

        public override void Initialize()
        {
            this.CPU.ExecutedInstruction += this.CPU_ExecutedInstruction;
            if (this._disassembling)
            {
                this.CPU.ExecutingInstruction += this.CPU_ExecutingInstruction;
            }
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.CPU.RaisePOWER();
            this.CPU.LowerRESET();
            this.CPU.RaiseINT();
            this.CPU.RaiseNMI();

            foreach (var expansion in this._expansions)
            {
                expansion.RaisePOWER();
            }
        }

        public override void LowerPOWER()
        {
            foreach (var expansion in this._expansions)
            {
                expansion.LowerPOWER();
            }

            this.CPU.LowerPOWER();
            base.LowerPOWER();
        }

        public void Plug(string path) => this.ROM.Load(path);

        public void Plug(Expansion expansion) => this._expansions.Add(expansion);

        public Expansion Expansion(int i) => this._expansions[i];

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

        private void CPU_ExecutedInstruction(object? sender, EventArgs e) => this.CPU.RaiseRESET();

        private void CPU_ExecutingInstruction(object? sender, System.EventArgs e)
        {
            Debug.Assert(this._disassembler is not null, "Disassembler has not been initialized.");
            var state = Z80.Disassembler.State(this.CPU);
            var disassembly = this._disassembler.Disassemble(this.CPU);
            System.Console.WriteLine($"{state} {disassembly}");
        }

        protected void RunCycle()
        {
            var taken = this.CPU.Run(++this._allowed);
            this._allowed -= taken;
        }
    }
}
