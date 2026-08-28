namespace SpectrumNet
{
    using SDL3;

    internal sealed class Cabinet(Configuration configuration) : Gaming.Game(configuration.LoggingLevel)
    {
        public Board Motherboard { get; } = new Board(configuration);

        public Configuration Settings { get; } = configuration;

        public void Plug(Expansion expansion) => this.Motherboard.Plug(expansion);

        public void Plug(string path) => this.Motherboard.Plug(path);

        public void LoadSna(string path) => this.Motherboard.LoadSna(path);

        public void LoadZ80(string path) => this.Motherboard.LoadZ80(path);

        public void InsertTape(string path) => this.Motherboard.InsertTape(path);

        protected override SDL.PixelFormat PixelFormat => ColorPalette.PixelFormat;

        public override float FramesPerSecond => Ula.FramesPerSecond;

        public override bool UseVSYNC => true;

        public override int DisplayScale => 2;

        public override int RasterWidth => Ula.RasterWidth;

        public override int RasterHeight => Ula.RasterHeight;

        public override string Title => "Spectrum";

        protected override uint[] Pixels() => this.Motherboard.ULA.Pixels;

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.Motherboard.RaisePOWER();
        }

        public override void LowerPOWER()
        {
            Motherboard.LowerPOWER();
            base.LowerPOWER();
        }

        public override void Initialise()
        {
            base.Initialise();
            this.Motherboard.Initialize();
        }

        protected override bool HandleJoyButtonDown(SDL.JoyButtonEvent e)
        {
            HandleJoyButtonDown(this.Joysticks(), e);
            return true;
        }

        protected override bool HandleJoyButtonUp(SDL.JoyButtonEvent e)
        {
            HandleJoyButtonUp(this.Joysticks(), e);
            return true;
        }

        protected override bool HandleGamepadButtonDown(SDL.GamepadButtonEvent e)
        {
            HandleGamepadButtonDown(this.Joysticks(), e);
            return true;
        }

        protected override bool HandleGamepadButtonUp(SDL.GamepadButtonEvent e)
        {
            HandleGamepadButtonUp(this.Joysticks(), e);
            return true;
        }

        private static void HandleJoyButtonDown(List<Joystick> joysticks, SDL.JoyButtonEvent e)
        {
            switch ((SDL.GamepadButton)e.Button)
            {
                case SDL.GamepadButton.South:
                    foreach (var joystick in joysticks)
                        joystick.PushFire();
                    break;
            }
        }

        private static void HandleJoyButtonUp(List<Joystick> joysticks, SDL.JoyButtonEvent e)
        {
            switch ((SDL.GamepadButton)e.Button)
            {
                case SDL.GamepadButton.South:
                    foreach (var joystick in joysticks)
                        joystick.ReleaseFire();
                    break;
            }
        }

        private static void HandleGamepadButtonDown(List<Joystick> joysticks, SDL.GamepadButtonEvent e)
        {
            switch ((SDL.GamepadButton)e.Button)
            {
                case SDL.GamepadButton.DPadUp:
                    foreach (var joystick in joysticks)
                        joystick.PushUp();
                    break;
                case SDL.GamepadButton.DPadDown:
                    foreach (var joystick in joysticks)
                        joystick.PushDown();
                    break;
                case SDL.GamepadButton.DPadLeft:
                    foreach (var joystick in joysticks)
                        joystick.PushLeft();
                    break;
                case SDL.GamepadButton.DPadRight:
                    foreach (var joystick in joysticks)
                        joystick.PushRight();
                    break;
            }
        }

        private static void HandleGamepadButtonUp(List<Joystick> joysticks, SDL.GamepadButtonEvent e)
        {
            switch ((SDL.GamepadButton)e.Button)
            {
                case SDL.GamepadButton.DPadUp:
                    foreach (var joystick in joysticks)
                        joystick.ReleaseUp();
                    break;
                case SDL.GamepadButton.DPadDown:
                    foreach (var joystick in joysticks)
                        joystick.ReleaseDown();
                    break;
                case SDL.GamepadButton.DPadLeft:
                    foreach (var joystick in joysticks)
                        joystick.ReleaseLeft();
                    break;
                case SDL.GamepadButton.DPadRight:
                    foreach (var joystick in joysticks)
                        joystick.ReleaseRight();
                    break;
            }
        }

        private List<Joystick> Joysticks()
        {
            List<Joystick> returned = [];
            for (int i = 0; i != this.Motherboard.NumberOfExpansions; ++i)
            {
                var expansion = this.Motherboard.Expansion(i);
                if (expansion.ExpansionType == Expansion.Type.Joystick)
                {
                    var joystick = (Joystick)expansion;
                    returned.Add(joystick);
                }
            }
            return returned;
        }

        protected override bool HandleKeyDown(SDL.Keycode key)
        {
            var handled = base.HandleKeyDown(key);
            if (!handled)
            {
                switch (key)
                {
                    case SDL.Keycode.F7:
                    case SDL.Keycode.F8:
                    case SDL.Keycode.F10:
                    case SDL.Keycode.F11:
                        handled = true;
                        break;
                }
                Motherboard.ULA.PokeKey(key);
            }
            return handled;
        }

        protected override bool HandleKeyUp(SDL.Keycode key)
        {
            var handled = base.HandleKeyUp(key);
            if (!handled)
            {
                Motherboard.ULA.PullKey(key);
            }
            return handled;
        }

        protected override void RunRasterLines() =>this.Motherboard.RenderLines();
    }
}
