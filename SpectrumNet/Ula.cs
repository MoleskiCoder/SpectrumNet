namespace SpectrumNet
{
    using EightBit;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;

    internal sealed class Ula : EightBit.ClockedChip
    {
        public const int VerticalRetraceLines = 16;
        public const int RasterWidth = (HorizontalRasterBorder * 2) + ActiveRasterWidth;
        public const int RasterHeight = UpperRasterBorder + ActiveRasterHeight + LowerRasterBorder;
        public const int TotalHeight = VerticalRetraceLines + RasterHeight;

        public const int CyclesPerSecond = 3500000; // 3.5Mhz
        public const float FramesPerSecond = 50.08f;

        private const int UpperRasterBorder = 48;
        private const int ActiveRasterHeight = 192;
        private const int LowerRasterBorder = 56;

        private const int HorizontalRasterBorder = 48;
        private const int ActiveRasterWidth = 256;

        private const int BytesPerLine = ActiveRasterWidth / 8;

        private const int AttributeAddress = 0x1800;

        private readonly ushort[] scanLineAddresses = new ushort[256];
        private readonly ushort[] attributeAddresses = new ushort[256];
        private readonly ColorPalette palette;
        private bool flash;
        private int frameCounter;
        private Color borderColour;

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

            this.BUS.Ports.ReadingPort += this.Ports_ReadingPort;
            this.BUS.Ports.WrittenPort += this.Ports_WrittenPort;
        }

        public event EventHandler<SteppingEventArgs>? Proceed;

        public static TimeSpan FrameLength => TimeSpan.FromSeconds(1 / FramesPerSecond);

        public void UpdateBorder(int value) => this.borderColour = this.palette.GetColor(value, false);

        public Color[] Pixels { get; } = new Color[RasterWidth * RasterHeight];

        private int FrameCycles { get; set; }

        private Board BUS { get; }

        public void RenderLine(int y)
        {
            // Vertical retrace
            if ((y & (int)~Mask.Four) == 0)
            {
                if (y == 0)
                {
                    this.StartFrame();  // Start of vertical retrace
                }

                this.Tick(RasterWidth);
            }

            // Upper border
            else if ((y & (int)~Mask.Six) == 0)
            {
                this.RenderBlankLine(y - VerticalRetraceLines);
            }

            // Rendered from Spectrum VRAM
            else if ((y & (int)~Mask.Eight) == 0)
            {
                this.RenderActiveLine(y - VerticalRetraceLines);
            }

            // Lower border
            else
            {
                this.RenderBlankLine(y - VerticalRetraceLines);
            }
        }

        public void PokeKey(Keys raw) => this.keyboardRaw.Add(raw);

        public void PullKey(Keys raw) => this.keyboardRaw.Remove(raw);

        protected override void OnRaisedPOWER()
        {
            this.frameCounter = 0;
            this.UpdateBorder(0);
            this.flash = false;
            base.OnRaisedPOWER();
        }

        protected override void OnTicked()
        {
            var available = this.Cycles / 2;
            if (available > 0)
            {
                this.Proceed?.Invoke(this, new SteppingEventArgs(available));
                this.FrameCycles += available;
                this.ResetCycles();
            }
            base.OnTicked();
        }

        private int IncrementFrameCounter()
        {
            if ((++this.frameCounter & (int)Mask.Four) == 0)
            {
                this.frameCounter = 0;
            }

            return this.frameCounter;
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

        private static bool UsedPort(byte port) => (port & (byte)EightBit.Bits.Bit0) == 0;

        private void MaybeReadingPort(byte port)
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

        private void ReadingPort(byte port)
        {
            var portHigh = this.BUS.Address.High;
            var selected = this.FindSelectedKeys((byte)~portHigh);
            var value = selected | (this.ear.Raised() ? Bit(6) : 0);
            this.BUS.Ports.WriteInputPort(port, (byte)value);
        }

        private void MaybeWrittenPort(byte port)
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

        private void WrittenPort(byte port)
        {
            var value = this.BUS.Ports.ReadOutputPort(port);

            this.mic.Match(value & (byte)Bits.Bit3);
            this.speaker.Match(value & (byte)Bits.Bit4);

            this.UpdateBorder(value & (byte)Mask.Three);

            this.BUS.Sound.Buzz(this.speaker, this.FrameCycles);
        }

        private void StartFrame()
        {
            this.BUS.Sound.EndFrame();
            this.FrameCycles = 0;
            if (this.IncrementFrameCounter() == 0)
            {
                this.Flash();
            }

            this.BUS.CPU.LowerINT();
        }

        private void Flash() => this.flash = !this.flash;

        private void RenderBlankLine(int y) => this.RenderHorizontalBorder(0, y, RasterWidth);

        private void RenderActiveLine(int y)
        {
            this.RenderLeftHorizontalBorder(y);
            this.RenderVRAM(y - UpperRasterBorder);
            this.RenderRightHorizontalBorder(y);
        }

        private void RenderLeftHorizontalBorder(int y) => this.RenderHorizontalBorder(0, y, HorizontalRasterBorder);

        private void RenderRightHorizontalBorder(int y) => this.RenderHorizontalBorder(HorizontalRasterBorder + ActiveRasterWidth, y, HorizontalRasterBorder);

        private void RenderHorizontalBorder(int x, int y, int width)
        {
            var begin = (y * RasterWidth) + x;
            for (var i = 0; i < width; ++i)
            {
                this.Pixels[begin + i] = this.borderColour;
                this.Tick();
            }
        }

        private void RenderVRAM(int y)
        {
            // Position in VRAM
            var bitmapAddressY = this.scanLineAddresses[y];
            var attributeAddressY = this.attributeAddresses[y];

            // Position in pixel render 
            var pixelBase = HorizontalRasterBorder + ((y + UpperRasterBorder) * RasterWidth);

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

                var background = this.palette.GetColor(flashing && this.flash ? ink : paper, bright);
                var foreground = this.palette.GetColor(flashing && this.flash ? paper : ink, bright);

                for (var bit = 0; bit < 8; ++bit)
                {
                    var pixel = (bitmap & Bit(bit)) != 0;
                    var x = (~bit & (int)Mask.Three) | (currentByte << 3);

                    this.Pixels[pixelBase + x] = pixel ? foreground : background;

                    this.Tick();
                }
            }
        }

        private void Ports_ReadingPort(object? sender, PortEventArgs e) => this.MaybeReadingPort(e.Port);

        private void Ports_WrittenPort(object? sender, PortEventArgs e) => this.MaybeWrittenPort(e.Port);
    }
}
