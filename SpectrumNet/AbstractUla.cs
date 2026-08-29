namespace SpectrumNet
{
    using EightBit;
    using System;

    internal abstract class AbstractUla<ColorT, KeyT> : EightBit.ClockedChip
    {
        private const int LeftRasterBorder = 32;
        private const int RightRasterBorder = 64;
        private const int TopRasterBorder = 56;
        private const int BottomRasterBorder = 56;

        private const int ActiveRasterWidth = 256;
        private const int ActiveRasterHeight = 192;

        private const int HorizontalRetraceClocks = 96;
        private const int VerticalRetraceLines = 8;

        private const int InterruptDuration = 64;   // 32 CPU cycles

        private const int BytesPerLine = ActiveRasterWidth / 8;
        private const ushort AttributeAddress = 0x1800;

        public const float FramesPerSecond = 50.08f;
        public const int UlaClockRate = 7000000; // 7Mhz
        public const int CpuClockRate = UlaClockRate / 2; // 3.5Mhz

        public const int RasterWidth = LeftRasterBorder + ActiveRasterWidth + RightRasterBorder;
        public const int RasterHeight = TopRasterBorder + ActiveRasterHeight + BottomRasterBorder;

        public const int TotalHeight = VerticalRetraceLines + RasterHeight;
        public const int TotalHorizontalClocks = HorizontalRetraceClocks + RasterWidth;

        private readonly ushort[] _scanLineAddresses = new ushort[256];
        private readonly ushort[] _attributeAddresses = new ushort[256];

        protected abstract AbstractColorPalette<ColorT> Palette { get; }

        private bool _flashing;
        private int _frameCounter;   // 4 bits
        private int _verticalCounter; // 9 bits
        private int _horizontalCounter; // 9 bits
        protected ColorT _borderColour;

        private int _contention;
        bool _accessingVRAM;

        // Output port information
        private EightBit.PinLevel _mic = EightBit.PinLevel.Low; // Bit 3
        private EightBit.PinLevel _speaker = EightBit.PinLevel.Low; // Bit 4

        // Input port information
        private EightBit.PinLevel _ear = EightBit.PinLevel.Low; // Bit 6

        protected readonly Dictionary<byte, KeyT[]> _keyboardMapping = [];
        private readonly HashSet<KeyT> _keyboardRaw = [];

        protected AbstractUla(Board bus)
        {
            this.BUS = bus ?? throw new ArgumentNullException(nameof(bus));

            this.RaisedPOWER += this.Ula_RaisedPOWER;

            this.Ticked += this.Ula_Ticked;

            this.BUS.CPU.LoweringRD += this.CPU_LoweringRD;
            this.BUS.CPU.LoweringWR += this.CPU_LoweringWR;

            this.BUS.Ports.ReadingPort += this.Ports_ReadingPort;
            this.BUS.Ports.WrittenPort += this.Ports_WrittenPort;
        }

        private void CPU_LoweringWR(object? sender, EventArgs e) => this.MaybeContend();

        private void CPU_LoweringRD(object? sender, EventArgs e) => this.MaybeContend();

        private bool MaybeContend() => this.MaybeContend(this.BUS.Address.Joined);

        private bool MaybeContend(ushort address)
        {
	        bool hit = this._accessingVRAM && Contended(address);
	        if (hit)
                this.AddContention(3);
	        return hit;
        }

        private static bool Contended(ushort address)
        {
	        // Contended area is between 0x4000 (0100000000000000)
	        //						and  0x7fff (0111111111111111)
	        var mask = Bits.Bit15 | Bits.Bit14;
            var masked = address & (ushort)mask;
	        return masked == 0b0100000000000000;
        }

        private void AddContention(int cycles) => this._contention += 2 * cycles;

        private bool MaybeApplyContention()
        {
	        var apply = this.Contention > 0;
	        if (apply)
		        --this._contention;
	        return apply;
        }

        private void InitialiseVRAMAddresses()
        {
            var line = 0;
            for (var p = 0; p < 4; ++p)
            {
                for (var y = 0; y < 8; ++y)
                {
                    for (var o = 0; o < 8; ++o, ++line)
                    {
                        this._scanLineAddresses[line] = (ushort)((p << 11) + (y << 5) + (o << 8));
                        this._attributeAddresses[line] = (ushort)(AttributeAddress + (((p << 3) + y) << 5));
                    }
                }
            }
        }

        public event EventHandler<EventArgs>? Proceed;

        public void SetBorder(int value) => this._borderColour = this.Palette.GetColor(value);

        public ColorT[] Pixels { get; } = new ColorT[RasterWidth * RasterHeight];

        private int Contention => this._contention;

        private int FrameUlaCycles => TotalHorizontalClocks * this.V + this.C;
        private int FrameCpuCycles => this.FrameUlaCycles / 2;

        private Board BUS { get; }

        private ref int F => ref this._frameCounter;

        private ref int V => ref this._verticalCounter;

        private ref int C => ref this._horizontalCounter;

        private void ProcessActiveLine() => this.ProcessActiveLine(this.V + TopRasterBorder);

        private void ProcessActiveLine(int y)
        {
            this.RenderVRAM(y);
            this.RenderRightRasterBorder(y);
            this.Tick(HorizontalRetraceClocks);
            this.RenderLeftRasterBorder(y);
        }

        private void ProcessBottomBorder() => this.ProcessBorder(this.V + TopRasterBorder);

        private void ProcessVerticalSync() => this.ProcessVerticalSync(this.V);

        private void ProcessVerticalSync(int y)
        {
            if (y == (ActiveRasterHeight + BottomRasterBorder))
                this.BUS.CPU.LowerINT();

            this.Tick(InterruptDuration);
            this.BUS.CPU.RaiseINT();
            this.Tick(ActiveRasterWidth - InterruptDuration);

            this.Tick(RightRasterBorder);
            this.Tick(HorizontalRetraceClocks);
            this.Tick(LeftRasterBorder);
        }

        private void ProcessTopBorder()
        {
            this.ProcessBorder(this.V - VerticalRetraceLines - TopRasterBorder - ActiveRasterHeight);
        }

        private void ProcessBorder(int y)
        {
            this.RenderRasterBorder(LeftRasterBorder, y, ActiveRasterWidth);
            this.RenderRightRasterBorder(y);
            this.Tick(HorizontalRetraceClocks);
            this.RenderLeftRasterBorder(y);
        }

        private void RenderLeftRasterBorder(int y) => this.RenderRasterBorder(0, y, LeftRasterBorder);

        private void RenderRightRasterBorder(int y) => this.RenderRasterBorder(LeftRasterBorder + ActiveRasterWidth, y, RightRasterBorder);

        private void RenderRasterBorder(int x, int y, int width)
        {
            // The ZX Spectrum ULA, Chris Smith
            // Chapter 12 (Generating the Display), Border Generation
            System.Diagnostics.Debug.Assert(x % 8 == 0);
            System.Diagnostics.Debug.Assert(width % 8 == 0);
            var chunks = width / 8;
            var offset = y * RasterWidth + x;
            for (int chunk = 0; chunk < chunks; ++chunk)
            {
                var colour = this._borderColour;
                for (int pixel = 0; pixel < 8; ++pixel)
                {
                    this.SetClockedPixel(offset++, colour);
                }
            }
        }

        public void RenderLine()
        {
            System.Diagnostics.Debug.Assert(this.C == 0);

            if (this.V < ActiveRasterHeight)
                this.ProcessActiveLine();

            else if (this.V < (ActiveRasterHeight + BottomRasterBorder))
                this.ProcessBottomBorder();

            else if (this.V < (ActiveRasterHeight + BottomRasterBorder + VerticalRetraceLines))
                this.ProcessVerticalSync();

            else if (this.V < (RasterHeight + VerticalRetraceLines))
                this.ProcessTopBorder();

            System.Diagnostics.Debug.Assert(this.C == TotalHorizontalClocks);
            this.IncrementV();
        }

        public void RenderLines()
        {
            System.Diagnostics.Debug.Assert(this.V == 0);
            for (int i = 0; i < TotalHeight; ++i)
                this.RenderLine();
            System.Diagnostics.Debug.Assert(this.V == TotalHeight);
            this.ResetV();
            this.BUS.Sound.EndFrame();
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

        private void Ula_RaisedPOWER(object? sender, EventArgs e)
        {
            this.InitialiseKeyboardMapping();
            this.InitialiseVRAMAddresses();

            this.ResetF();
            this.ResetV();
            this.C = 0;
            this.SetBorder(0);
            this._flashing = false;
        }

        private void Ula_Ticked(object? sender, EventArgs e)
        {
            ++this.C;
            if ((this.Cycles % 2) == 0)
            {
                if (!this.MaybeApplyContention())
                    this.Proceed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected abstract void InitialiseKeyboardMapping();

        private byte FindSelectedKeys(byte rows)
        {
            var returned = 0xff;
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
            this.BUS.Ports.WriteInputPort(port, (byte)value);
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
            var value = this.BUS.Ports.ReadOutputPort(port);

            this._mic.Match(value & (byte)Bits.Bit3);
            this._speaker.Match(value & (byte)Bits.Bit4);

            this.SetBorder(value & (byte)Mask.Three);

            this.BUS.Sound.Buzz(this._speaker, this.FrameCpuCycles);
        }

        private void Flash() => this._flashing = !this._flashing;

        private void RenderVRAM(int y)
        {
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < RasterHeight);

            this._accessingVRAM = true;

	        // Position in VRAM
	        var indexY = y - TopRasterBorder;
            System.Diagnostics.Debug.Assert(indexY < ActiveRasterHeight);
            var bitmapAddressY = this._scanLineAddresses[indexY];
            var attributeAddressY = this._attributeAddresses[indexY];

            // Position in pixel render 
            var pixelBase = LeftRasterBorder + (y * RasterWidth);

            var bitmapAddress = bitmapAddressY;
            var attributeAddress = attributeAddressY;

            for (var currentByte = 0; currentByte < BytesPerLine; ++currentByte)
            {
                var attribute = this.BUS.VRAM.Peek(attributeAddress++);
                var ink = attribute & (byte)Mask.Three;
                var paper = (attribute >> 3) & (int)Mask.Three;
                var bright = (attribute & (byte)Bits.Bit6) != 0;
                var flashing = (attribute & (byte)Bits.Bit7) != 0;
                var background = this.Palette.GetColor(flashing && this._flashing ? ink : paper, bright);
                var foreground = this.Palette.GetColor(flashing && this._flashing ? paper : ink, bright);

                var bitmap = this.BUS.VRAM.Peek(bitmapAddress++);
                var byteX = currentByte << 3;
		        for (int bit = 0; bit< 8; ++bit)
                {
                    var pixel = (bitmap & Bit(bit)) != 0;
                    var x = (~bit & (int)Mask.Three) | byteX;

                    this.SetClockedPixel(pixelBase + x, pixel ? foreground : background);
                }
            }
            this._accessingVRAM = false;
        }

        private void SetClockedPixel(int offset, ColorT colour)
        {
            this.SetPixel(offset, colour);
            this.Tick();
        }

        private void SetPixel(int offset, ColorT colour) => this.Pixels[offset] = colour;

        private void Ports_ReadingPort(object? sender, PortEventArgs e) => this.MaybeReadingPort(e.Port);

        private void Ports_WrittenPort(object? sender, PortEventArgs e) => this.MaybeWrittenPort(e.Port);
    }
}
