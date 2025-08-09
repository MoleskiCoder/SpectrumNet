namespace SpectrumNet
{
    using EightBit;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using System;

    internal sealed class Ula : EightBit.ClockedChip
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
        private const int AttributeAddress = 0x1800;

        public const float FramesPerSecond = 50.08f;
        public const int UlaClockRate = 7000000; // 7Mhz
        public const int CpuClockRate = UlaClockRate / 2; // 3.5Mhz

        public const int RasterWidth = LeftRasterBorder + ActiveRasterWidth + RightRasterBorder;
        public const int RasterHeight = TopRasterBorder + ActiveRasterHeight + BottomRasterBorder;

        public const int TotalHeight = VerticalRetraceLines + RasterHeight;
        public const int TotalHorizontalClocks = HorizontalRetraceClocks + RasterWidth;
        public const int TotalFrameClocks = TotalHeight * TotalHorizontalClocks;
        public const float CalculatedClockFrequency = TotalFrameClocks * FramesPerSecond;

        private readonly int[] scanLineAddresses = new int[256];
        private readonly int[] attributeAddresses = new int[256];
        private readonly ColorPalette palette;
        private bool flashing;
        private int frameCounter;   // 4 bits
        private int verticalCounter; // 9 bits
        private int horizontalCounter; // 9 bits
        private Color borderColour;
        private int contention;
        bool accessingVRAM;

        // Output port information
        private EightBit.PinLevel mic = EightBit.PinLevel.Low; // Bit 3
        private EightBit.PinLevel speaker = EightBit.PinLevel.Low; // Bit 4

        // Input port information
        private EightBit.PinLevel ear = EightBit.PinLevel.Low; // Bit 6

        private readonly Dictionary<byte, Keys[]> keyboardMapping = [];
        private readonly HashSet<Keys> keyboardRaw = [];

        public Ula(ColorPalette palette, Board bus)
        {
            this.palette = palette ?? throw new ArgumentNullException(nameof(palette));
            this.BUS = bus ?? throw new ArgumentNullException(nameof(bus));

            this.InitialiseKeyboardMapping();
            this.InitialiseVRAMAddresses();

            this.RaisedPOWER += this.Ula_RaisedPOWER;

            this.Ticked += this.Ula_Ticked;

            this.BUS.CPU.LoweringRD += this.CPU_LoweringRD;
            this.BUS.CPU.LoweringWR += this.CPU_LoweringWR;

            this.BUS.Ports.ReadingPort += this.Ports_ReadingPort;
            this.BUS.Ports.WrittenPort += this.Ports_WrittenPort;
        }

        private void CPU_LoweringWR(object? sender, EventArgs e)
        {
            this.MaybeContend();
        }

        private void CPU_LoweringRD(object? sender, EventArgs e)
        {
            this.MaybeContend();
        }

        private bool MaybeContend()
        {
	        return this.MaybeContend(this.BUS.Address.Word);
        }

        private bool MaybeContend(ushort address)
        {
	        bool hit = this.accessingVRAM && Contended(address);
	        if (hit)
                this.AddContention(3);
	        return hit;
        }

        private static bool Contended(ushort address)
        {
	        // Contended area is between 0x4000 (0100000000000000)
	        //						and  0x7fff (0111111111111111)
	        var mask = (Bits.Bit15 | Bits.Bit14);
            var masked = address & (ushort)mask;
	        return masked == 0b0100000000000000;
        }

        private void AddContention(int cycles)
        {
	        this.contention += 2 * cycles;
        }

        private bool MaybeApplyContention()
        {
	        var apply = this.Contention > 0;
	        if (apply)
		        --this.contention;
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
                        this.scanLineAddresses[line] = (ushort)((p << 11) + (y << 5) + (o << 8));
                        this.attributeAddresses[line] = (ushort)(AttributeAddress + (((p << 3) + y) << 5));
                    }
                }
            }
        }

        public event EventHandler<EventArgs>? Proceed;

        public static TimeSpan FrameLength => TimeSpan.FromSeconds(1 / FramesPerSecond);

        public void UpdateBorder(int value) => this.borderColour = this.palette.GetColor(value, false);

        public Color[] Pixels { get; } = new Color[RasterWidth * RasterHeight];

        private int Contention => this.contention;

        private int FrameUlaCycles => TotalHorizontalClocks * this.V + this.C;
        private int FrameCpuCycles => this.FrameUlaCycles / 2;

        private Board BUS { get; }

        private int F => this.frameCounter;

        private int V => this.verticalCounter;

        private int C => this.horizontalCounter;

        private void ProcessActiveLine()
        {
            this.ProcessActiveLine(this.V + TopRasterBorder);
        }

        private void ProcessActiveLine(int y)
        {
            this.RenderVRAM(y);
            this.RenderRightRasterBorder(y);
            this.Tick(HorizontalRetraceClocks);
            this.RenderLeftRasterBorder(y);
        }

        private void ProcessBottomBorder()
        {
            this.ProcessBorder(this.V + TopRasterBorder);
        }

        private void ProcessVerticalSync()
        {
            this.ProcessVerticalSync(this.V);
        }

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

        private void RenderLeftRasterBorder(int y)
        {
            this.RenderRasterBorder(0, y, LeftRasterBorder);
        }

        private void RenderRightRasterBorder(int y)
        {
            this.RenderRasterBorder(LeftRasterBorder + ActiveRasterWidth, y, RightRasterBorder);
        }

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
                var colour = this.borderColour;
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

        private void ResetF()
        {
            this.frameCounter = 0;
        }

        private void IncrementF()
        {
            if ((++this.frameCounter & (int)Mask.Four) == 0)
            {
                this.frameCounter = 0;
                this.Flash();
            }
        }

        private void ResetC()
        {
            this.horizontalCounter = 0;
        }

        private void IncrementC()
        {
            if ((++this.horizontalCounter & (int)Mask.Nine) == 0)
                this.horizontalCounter = 0;
        }

        private void ResetV()
        {
            this.verticalCounter = 0;
            this.IncrementF();
        }

        private void IncrementV()
        {
            if ((++this.verticalCounter & (int)Mask.Nine) == 0)
                this.verticalCounter = 0;
            this.ResetC();
        }

        public void PokeKey(Keys raw) => this.keyboardRaw.Add(raw);

        public void PullKey(Keys raw) => this.keyboardRaw.Remove(raw);

        private void Ula_RaisedPOWER(object? sender, EventArgs e)
        {
            this.ResetF();
            this.ResetV();
            this.ResetC();
            this.UpdateBorder(0);
            this.flashing = false;
        }

        private void Ula_Ticked(object? sender, EventArgs e)
        {
            this.IncrementC();
            if ((this.Cycles % 2) == 0)
            {
                if (!this.MaybeApplyContention())
                    this.Proceed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void InitialiseKeyboardMapping()
        {
            // Left side
            this.keyboardMapping[Bit(0)] = [Keys.LeftShift, Keys.Z,             Keys.X,         Keys.C,         Keys.V];
            this.keyboardMapping[Bit(1)] = [Keys.A,         Keys.S,             Keys.D,         Keys.F,         Keys.G];
            this.keyboardMapping[Bit(2)] = [Keys.Q,         Keys.W,             Keys.E,         Keys.R,         Keys.T];
            this.keyboardMapping[Bit(3)] = [Keys.D1,        Keys.D2,            Keys.D3,        Keys.D4,        Keys.D5];

            // Right side
            this.keyboardMapping[Bit(4)] = [Keys.D0,        Keys.D9,            Keys.D8,        Keys.D7,        Keys.D6];
            this.keyboardMapping[Bit(5)] = [Keys.P,         Keys.O,             Keys.I,         Keys.U,         Keys.Y];
            this.keyboardMapping[Bit(6)] = [Keys.Enter,     Keys.L,             Keys.K,         Keys.J,         Keys.H];
            this.keyboardMapping[Bit(7)] = [Keys.Space,     Keys.RightShift,    Keys.M,         Keys.N,         Keys.B];
        }

        private byte FindSelectedKeys(byte rows)
        {
            var returned = 0xff;
            for (var row = 0; row < 8; ++row)
            {
                var current = Bit(row);
                if (((rows & current) != 0) && this.keyboardMapping.TryGetValue(current, out var keys))
                {
                    for (var column = 0; column < 5; ++column)
                    {
                        if (this.keyboardRaw.Contains(keys[column]))
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
            var value = selected | (this.ear.Raised() ? Bit(6) : 0);
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

            this.mic.Match(value & (byte)Bits.Bit3);
            this.speaker.Match(value & (byte)Bits.Bit4);

            this.UpdateBorder(value & (byte)Mask.Three);

            this.BUS.Sound.Buzz(this.speaker, this.FrameCpuCycles);
        }

        private void Flash() => this.flashing = !this.flashing;

        private void RenderVRAM(int y)
        {
            System.Diagnostics.Debug.Assert(y >= 0);
            System.Diagnostics.Debug.Assert(y < RasterHeight);

            this.accessingVRAM = true;

	        // Position in VRAM
	        var addressY = y - TopRasterBorder;
            System.Diagnostics.Debug.Assert(addressY<ActiveRasterHeight);
            var bitmapAddressY = this.scanLineAddresses[addressY];
            var attributeAddressY = this.attributeAddresses[addressY];

            // Position in pixel render 
            var pixelBase = LeftRasterBorder + (y * RasterWidth);

            for (var currentByte = 0; currentByte < BytesPerLine; ++currentByte)
            {
                var bitmapAddress = bitmapAddressY + currentByte;
                var bitmap = this.BUS.VRAM.Peek((ushort)bitmapAddress);

                var attributeAddress = attributeAddressY + currentByte;
                var attribute = this.BUS.VRAM.Peek((ushort)attributeAddress);

                var ink = attribute & (byte)Mask.Three;
                var paper = (attribute >> 3) & (int)Mask.Three;
                var bright = (attribute & (byte)Bits.Bit6) != 0;
                var flashing = (attribute & (byte)Bits.Bit7) != 0;

                var background = this.palette.GetColor(flashing && this.flashing ? ink : paper, bright);
                var foreground = this.palette.GetColor(flashing && this.flashing ? paper : ink, bright);

                var byteX = currentByte << 3;
		        for (int bit = 0; bit< 8; ++bit)
                {
                    var pixel = (bitmap & Bit(bit)) != 0;
                    var x = (~bit & (int)Mask.Three) | byteX;

                    this.SetClockedPixel(pixelBase + x, pixel? foreground : background);
                }
            }
            this.accessingVRAM = false;
        }

        private void SetClockedPixel(int offset, Color colour)
        {
            this.SetPixel(offset, colour);
            this.Tick();
        }

        private void SetPixel(int offset, Color colour)
        {
            this.Pixels[offset] = colour;
        }

        private void Ports_ReadingPort(object? sender, PortEventArgs e) => this.MaybeReadingPort(e.Port);

        private void Ports_WrittenPort(object? sender, PortEventArgs e) => this.MaybeWrittenPort(e.Port);
    }
}
