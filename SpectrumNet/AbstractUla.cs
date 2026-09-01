namespace SpectrumNet
{
    using EightBit;
    using System;
    using System.Diagnostics;

    internal abstract class AbstractUla<ColorT, KeyT> : EightBit.ClockedChip
    {
        private const int CharactersPerLine = ITimings.ActiveRasterWidth / PixelsPerCharacter;

        public const int InterruptDuration = 64;   // 32 CPU cycles

        public const int PixelsPerCharacter = 8;

        private const ushort AttributeAddress = 0x1800;     // Offset in VRAM for attributes (VRAM starts at 0x4000, so attributes are at 0x5800)

        private readonly Bus _bus;
        private readonly ITimings _timings;
        private readonly Z80.Z80 _cpu;
        private readonly InputOutput _ports;
        private readonly Ram _vram;
        private readonly AbstractBuzzer _buzzer;

        private ColorT[]? _pixels;

        private bool _flashing;
        private int _frameCounter;   // 4 bits
        private int _verticalCounter; // 9 bits
        private int _horizontalCounter; // 9 bits
        protected ColorT? _borderColour;

        private int _contention;
        private int _interruptCycles;

        // Output port information
        private EightBit.PinLevel _mic = EightBit.PinLevel.Low; // Bit 3
        private EightBit.PinLevel _speaker = EightBit.PinLevel.Low; // Bit 4

        // Input port information
        private EightBit.PinLevel _ear = EightBit.PinLevel.Low; // Bit 6

        protected readonly Dictionary<byte, KeyT[]> _keyboardMapping = [];
        private readonly HashSet<KeyT> _keyboardRaw = [];

        protected abstract AbstractColorPalette<ColorT> Palette { get; }

        private bool ContendedAddress => Contended(this._bus.Address.Joined);

        public ColorT[]? Pixels => this._pixels;

        public int FrameUlaCycles => this._timings.TotalHorizontalClocks * this.V + this.C;
        public int FrameCpuCycles => this.FrameUlaCycles / 2;

        public bool Flashing => this._flashing;

        public ref int F => ref this._frameCounter;

        public ref int V => ref this._verticalCounter;

        public ref int C => ref this._horizontalCounter;

        public event EventHandler<EventArgs>? Proceed;

        protected AbstractUla(EightBit.Bus bus, ITimings timings, Z80.Z80 cpu, InputOutput ports, Ram vram, AbstractBuzzer buzzer)
        {
            this._bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this._timings = timings ?? throw new ArgumentNullException(nameof(timings));
            this._cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
            this._ports = ports ?? throw new ArgumentNullException(nameof(ports));
            this._vram = vram ?? throw new ArgumentNullException(nameof(vram));
            this._buzzer = buzzer ?? throw new ArgumentNullException(nameof(buzzer));

            this.Ticked += this.Ula_Ticked;

            this._cpu.LoweringRD += this.CPU_LoweringRD;
            this._cpu.LoweringWR += this.CPU_LoweringWR;

            this._ports.ReadingPort += this.Ports_ReadingPort;
            this._ports.WrittenPort += this.Ports_WrittenPort;
        }

        private void Ula_Ticked(object? sender, EventArgs e)
        {
            ++this.C;
            if ((this.Cycles % 2) == 0)
            {
                ++this._interruptCycles;
                this.MaybeProceed();
            }
        }

        private void CPU_LoweringWR(object? sender, EventArgs e) => this.CalculateContention();

        private void CPU_LoweringRD(object? sender, EventArgs e) => this.CalculateContention();

        private void Ports_ReadingPort(object? sender, PortEventArgs e) => this.MaybeReadingPort(e.Port);

        private void Ports_WrittenPort(object? sender, PortEventArgs e) => this.MaybeWrittenPort(e.Port);

        private void MaybeProceed()
        {
            var contended = this._contention > 0;
            if (contended)
            {
                this._contention--;
            }
            else
            {
                this.Proceed?.Invoke(this, EventArgs.Empty);
            }
        }

        private static bool Contended(ushort address)
        {
            // Contended area is between 0x4000 (0100000000000000)
            //						and  0x7fff (0111111111111111)
            var mask = Bits.Bit15 | Bits.Bit14;
            var masked = address & (ushort)mask;
            return masked == 0b0100000000000000;
        }

        public void SetBorder(int value) => this._borderColour = this.Palette.GetColor(value);

        private void ProcessActiveLine(int y)
        {
            this.RenderLeftRasterBorder(y);
            this.RenderVRAM(y);
            this.RenderRightRasterBorder(y);
            this.Tick(ITimings.HorizontalRetraceClocks);
        }

        private void ProcessVerticalSync()
        {
            if (this.V == 0)
            {
                this._cpu.LowerINT();
                this._interruptCycles = 0;
            }

            this.Tick(InterruptDuration);
            this._cpu.RaiseINT();
            this.Tick(this._timings.LeftRasterBorder - InterruptDuration + ITimings.ActiveRasterWidth + this._timings.RightRasterBorder + ITimings.HorizontalRetraceClocks);
        }

        private void ProcessBorder(int y)
        {
            Debug.Assert(y >= 0);
            this.RenderRasterBorder(this._timings.LeftRasterBorder, y, ITimings.ActiveRasterWidth);
            this.RenderRightRasterBorder(y);
            this.Tick(ITimings.HorizontalRetraceClocks);
            this.RenderLeftRasterBorder(y);
        }

        private void RenderLeftRasterBorder(int y) => this.RenderRasterBorder(0, y, this._timings.LeftRasterBorder);

        private void RenderRightRasterBorder(int y) => this.RenderRasterBorder(this._timings.LeftRasterBorder + ITimings.ActiveRasterWidth, y, this._timings.RightRasterBorder);

        private void RenderRasterBorder(int x, int y, int width)
        {
            Debug.Assert(x >= 0);
            Debug.Assert(y >= 0);
            Debug.Assert(width > 0);
            // The ZX Spectrum ULA, Chris Smith
            // Chapter 12 (Generating the Display), Border Generation
            Debug.Assert(x % PixelsPerCharacter == 0);
            Debug.Assert(width % PixelsPerCharacter == 0);
            var chunks = width / PixelsPerCharacter;
            var offset = y * this._timings.RasterWidth + x;
            for (int chunk = 0; chunk < chunks; ++chunk)
            {
                var colour = this._borderColour;
                Debug.Assert(colour is not null);
                for (int pixel = 0; pixel < PixelsPerCharacter; ++pixel)
                {
                    this.SetClockedPixel(offset++, colour);
                }
            }
        }

        public void RenderLine()
        {
            Debug.Assert(this.C == 0);

            if (this.V < ITimings.VerticalRetraceLines)
                this.ProcessVerticalSync();
            else if (this.V < (ITimings.VerticalRetraceLines + this._timings.TopRasterBorder))
                this.ProcessBorder(this.V - ITimings.VerticalRetraceLines);
            else if (this.V < (ITimings.VerticalRetraceLines + this._timings.TopRasterBorder + ITimings.ActiveRasterHeight))
                this.ProcessActiveLine(this.V - ITimings.VerticalRetraceLines);
            else if (this.V < (ITimings.VerticalRetraceLines + this._timings.TopRasterBorder + ITimings.ActiveRasterHeight + this._timings.BottomRasterBorder))
                this.ProcessBorder(this.V - ITimings.VerticalRetraceLines);

            Debug.Assert(this.C == this._timings.TotalHorizontalClocks);
            this.IncrementV();
        }

        public void RenderLines()
        {
            Debug.Assert(this.V == 0);
            for (int i = 0; i < this._timings.TotalHeight; ++i)
                this.RenderLine();
            Debug.Assert(this.V == this._timings.TotalHeight);
            this.ResetV();
            this._buzzer.EndFrame();
        }

        private void IncrementF()
        {
            if ((++this.F & (int)Mask.Four) == 0)
            {
                this.ResetF();
            }
        }

        private void ResetF()
        {
            this.F = 0;
            this.Flash();
        }

        private void ResetV()
        {
            this.V = 0;
            this.IncrementF();
        }

        private void IncrementV()
        {
            ++this.V;
            this.C = 0;
        }

        public void PokeKey(KeyT raw) => this._keyboardRaw.Add(raw);

        public void PullKey(KeyT raw) => this._keyboardRaw.Remove(raw);

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this._pixels = new ColorT[this._timings.RasterWidth * this._timings.RasterHeight];
            this.InitialiseKeyboardMapping();
            this.ResetF();
            this.ResetV();
            this.C = 0;
            this.SetBorder((int)AbstractColorPalette<ColorT>.Index.Black);
            this._flashing = false;
        }

        private void CalculateContention()
        {
            this._contention = 0;
            var contendedBase = (ITimings.VerticalRetraceLines + this._timings.TopRasterBorder) * this._timings.TotalHorizontalClocks / 2 - 1;
            Debug.Assert(contendedBase == (this._timings is NtscTimings ? 8959 : 14335));
            var possiblyContended = (this._interruptCycles > contendedBase) && this.ContendedAddress;
            if (possiblyContended)
            {
                var contendedCycles = ITimings.ActiveRasterWidth / 2;
                var uncontendedCycles = (ITimings.HorizontalRetraceClocks + this._timings.LeftRasterBorder + this._timings.RightRasterBorder) / 2;
                var totalNumberOfCyclesPerLine = contendedCycles + uncontendedCycles;
                var currentCycle = this._interruptCycles - contendedBase;
                var scanLine = currentCycle / totalNumberOfCyclesPerLine;
                if (scanLine < ITimings.ActiveRasterHeight)
                {
                    var scanColumn = currentCycle % totalNumberOfCyclesPerLine;
                    if (scanColumn < contendedCycles)
                    {
                        int[] waitPattern = [6, 5, 4, 3, 2, 1, 0, 0];
                        this._contention = waitPattern[scanColumn % PixelsPerCharacter];
                    }
                }
            }
        }

        protected abstract void InitialiseKeyboardMapping();

        private byte FindSelectedKeys(byte rows)
        {
            var returned = (int)Mask.Eight;
            for (var row = 0; row < 8; ++row)
            {
                var current = Bit(row);
                if (((rows & current) != 0) && this._keyboardMapping.TryGetValue(current, out var keys))
                {
                    for (var column = 0; column < 5; ++column)
                    {
                        if (this._keyboardRaw.Contains(keys[column]))
                        {
                            returned &= ~Bit(column);
                        }
                    }
                }
            }

            return (byte)returned;
        }

        private static bool UsedPort(Register16 port) => (port.Low & (byte)EightBit.Bits.Bit0) == 0;

        private void MaybeReadingPort(Register16 port)
        {
            if (UsedPort(port))
            {
                this.ReadingPort(port);
            }
        }

        // 0 - 4	Keyboard Inputs(0 = Pressed, 1 = Released)
        // 5		Not used
        // 6		EAR Input(CAS LOAD)
        // 7		Not used
        // A8..A15	Keyboard Address Output(0 = Select)

        // 128 64 32 16  8  4  2  U
        //   7  6  5  4  3  2  1  0
        //            <----------->	Keyboard
        //         -				Not used
        //      -					Ear input
        //   -						Not used

        private void ReadingPort(Register16 port)
        {
            var portHigh = port.High;
            var selected = this.FindSelectedKeys((byte)~portHigh);
            var value = selected | (this._ear.Raised() ? Bit(6) : 0);
            this._ports.WriteInputPort(port, (byte)value);
        }

        private void MaybeWrittenPort(Register16 port)
        {
            if (UsedPort(port))
            {
                this.WrittenPort(port);
            }
        }

        // 0 - 2	Border Color(0..7) (always with Bright = off)
        // 3		MIC Output(CAS SAVE) (0 = On, 1 = Off)
        // 4		Beep Output(ULA Sound)    (0 = Off, 1 = On)
        // 5 - 7	Not used

        // 128 64 32 16  8  4  2  U
        //   7  6  5  4  3  2  1  0
        //                  <----->	Border colour
        //               -		    Mic output
        //            -				Beep output
        //   <----->				Not used

        private void WrittenPort(Register16 port)
        {
            var value = this._ports.ReadOutputPort(port);

            this._mic.Match(value & (byte)Bits.Bit3);
            this._speaker.Match(value & (byte)Bits.Bit4);

            this.SetBorder(value & (byte)Mask.Three);

            this._buzzer.Buzz(this._speaker, this.FrameCpuCycles);
        }

        private void Flash() => this._flashing = !this._flashing;

        private static ushort AttributeOffset(int line)
        {
            Debug.Assert(line < 192);
            var row = line >> 3;
            Debug.Assert(row < 24);
            return (ushort)(AttributeAddress + (row << 5));
        }

        private static ushort PixelOffset(int line)
        {
            Debug.Assert(line < 192);
            var scan = line & (int)Mask.Three;
            Debug.Assert(scan < PixelsPerCharacter);
            line >>= 3;
            var row = line & (int)Mask.Three;
            Debug.Assert(row < PixelsPerCharacter);
            line >>= 3;
            var chunk = line & (int)Mask.Two;
            Debug.Assert(chunk < 3);
            return (ushort)(chunk * 0x0800 + row * 0x20 + scan * 0x100);
        }

        private void RenderVRAM(int y)
        {
            // Check that incoming row is not in either the top or bottom border area
            // and is within the active raster height
            Debug.Assert(y >= 0);
            Debug.Assert(y < (this._timings.RasterHeight - this._timings.BottomRasterBorder));
            Debug.Assert(y >= this._timings.TopRasterBorder);

            var indexY = y - this._timings.TopRasterBorder;
            Debug.Assert(indexY >= 0);
            Debug.Assert(indexY < ITimings.ActiveRasterHeight);

            var bitmapAddress = PixelOffset(indexY);        // Starting pixel row position in VRAM
            var attributeAddress = AttributeOffset(indexY); // Starting attribute row position in VRAM

            // Position in pixel render 
            var pixelBase = this._timings.LeftRasterBorder + (y  * this._timings.RasterWidth);

            for (var currentCharacter = 0; currentCharacter < CharactersPerLine; ++currentCharacter)
            {
                var attribute = this._vram.Peek(attributeAddress++);
                var ink = attribute & (byte)Mask.Three;
                var paper = (attribute >> 3) & (int)Mask.Three;
                var bright = (attribute & (byte)Bits.Bit6) != 0;
                var flashing = (attribute & (byte)Bits.Bit7) != 0;
                var background = this.Palette.GetColor(flashing && this.Flashing ? ink : paper, bright);
                var foreground = this.Palette.GetColor(flashing && this.Flashing ? paper : ink, bright);

                var bitmap = this._vram.Peek(bitmapAddress++);
                var byteX = currentCharacter << 3;
                for (int bit = 0; bit < PixelsPerCharacter; ++bit)
                {
                    var pixel = (bitmap & Bit(bit)) != 0;
                    var x = (~bit & (int)Mask.Three) | byteX;

                    this.SetClockedPixel(pixelBase + x, pixel ? foreground : background);
                }
            }
        }

        private void SetClockedPixel(int offset, ColorT colour)
        {
            this.SetPixel(offset, colour);
            this.Tick();
        }

        private void SetPixel(int offset, ColorT colour)
        {
            Debug.Assert(this.Pixels is not null);
            this.Pixels[offset] = colour;
        }
    }
}
