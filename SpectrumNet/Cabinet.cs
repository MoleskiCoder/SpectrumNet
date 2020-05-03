namespace SpectrumNet
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Cabinet : Game
    {
        private const int DisplayScale = 2;
        private const int DisplayWidth = Ula.RasterWidth;
        private const int DisplayHeight = Ula.RasterHeight;

        private readonly ColorPalette palette = new ColorPalette();

        private readonly List<Keys> pressedKeys = new List<Keys>();
        private readonly Dictionary<PlayerIndex, GamePadButtons> pressedButtons = new Dictionary<PlayerIndex, GamePadButtons>();
        private readonly Dictionary<PlayerIndex, GamePadDPad> pressedDPad = new Dictionary<PlayerIndex, GamePadDPad>();

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D bitmapTexture;

        private bool disposed = false;

        public Cabinet(Configuration configuration)
        {
            this.Settings = configuration;
            this.Motherboard = new Board(this.palette, configuration);

            this.graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
            };

            this.pressedButtons[PlayerIndex.One] = new GamePadButtons();
            this.pressedButtons[PlayerIndex.Two] = new GamePadButtons();
            this.pressedDPad[PlayerIndex.One] = new GamePadDPad();
            this.pressedDPad[PlayerIndex.Two] = new GamePadDPad();
        }

        public event EventHandler<EventArgs> Initializing;

        public event EventHandler<EventArgs> Initialized;

        public Board Motherboard { get; }

        public Configuration Settings { get; }

        public void Plug(Expansion expansion) => this.Motherboard.Plug(expansion);

        public void Plug(string path) => this.Motherboard.Plug(path);

        public void LoadSna(string path) => this.Motherboard.LoadSna(path);

        public void LoadZ80(string path) => this.Motherboard.LoadZ80(path);

        private void OnInitializing() => this.Initializing?.Invoke(this, EventArgs.Empty);

        private void OnInitialized() => this.Initialized?.Invoke(this, EventArgs.Empty);

        protected override void Initialize()
        {
            this.OnInitializing();

            base.Initialize();

            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.bitmapTexture = new Texture2D(this.GraphicsDevice, DisplayWidth, DisplayHeight);
            this.ChangeResolution(DisplayWidth, DisplayHeight);
            this.palette.Load();

            this.Motherboard.Initialize();
            this.Motherboard.RaisePOWER();

            this.TargetElapsedTime = Ula.FrameLength;
            this.IsMouseVisible = false;

            this.OnInitialized();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!gameTime.IsRunningSlowly)
            {
                this.CheckGamePads();
                this.CheckKeyboard();
                this.RunFrame();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            this.DrawPixels();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
            this.Motherboard.LowerPOWER();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Motherboard?.Dispose();
                    this.bitmapTexture?.Dispose();
                    this.spriteBatch?.Dispose();
                    this.graphics?.Dispose();
                }

                this.disposed = true;
            }
        }

        private void CheckGamePads()
        {
            this.MaybeHandleGamePadOne();
        }

        private void MaybeHandleGamePadOne()
        {
            var capabilities = GamePad.GetCapabilities(PlayerIndex.One);
            if (capabilities.IsConnected && (capabilities.GamePadType == GamePadType.GamePad))
            {
                this.HandleGamePadOne();
            }
        }

        private void HandleGamePadOne()
        {
            var state = GamePad.GetState(PlayerIndex.One);

            var currentButtons = state.Buttons;
            var previousButtons = this.pressedButtons[PlayerIndex.One];

            var currentDPad = state.DPad;
            var previousDPad = this.pressedDPad[PlayerIndex.One];

            for (var i = 0; i < this.Motherboard.NumberOfExpansions; ++i)
            {
                var expansion = this.Motherboard.Expansion(i);
                var joystick = (Joystick)expansion;

                // Up

                if ((currentDPad.Up == ButtonState.Pressed) && (previousDPad.Up == ButtonState.Released))
                {
                    joystick.PushUp();
                }

                if ((currentDPad.Up == ButtonState.Released) && (previousDPad.Up == ButtonState.Pressed))
                {
                    joystick.ReleaseUp();
                }

                // Down

                if ((currentDPad.Down == ButtonState.Pressed) && (previousDPad.Down == ButtonState.Released))
                {
                    joystick.PushDown();
                }

                if ((currentDPad.Down == ButtonState.Released) && (previousDPad.Down == ButtonState.Pressed))
                {
                    joystick.ReleaseDown();
                }

                // Left

                if ((currentDPad.Left == ButtonState.Pressed) && (previousDPad.Left == ButtonState.Released))
                {
                    joystick.PushLeft();
                }

                if ((currentDPad.Left == ButtonState.Released) && (previousDPad.Left == ButtonState.Pressed))
                {
                    joystick.ReleaseLeft();
                }

                // Right

                if ((currentDPad.Right == ButtonState.Pressed) && (previousDPad.Right == ButtonState.Released))
                {
                    joystick.PushRight();
                }

                if ((currentDPad.Right == ButtonState.Released) && (previousDPad.Right == ButtonState.Pressed))
                {
                    joystick.ReleaseRight();
                }

                // Fire

                if ((currentButtons.A == ButtonState.Pressed) && (previousButtons.A == ButtonState.Released))
                {
                    joystick.PushFire();
                }

                if ((currentButtons.A == ButtonState.Released) && (previousButtons.A == ButtonState.Pressed))
                {
                    joystick.ReleaseFire();
                }
            }

            this.pressedButtons[PlayerIndex.One] = currentButtons;
            this.pressedDPad[PlayerIndex.One] = currentDPad;
        }

        private void CheckKeyboard()
        {
            var state = Keyboard.GetState();
            var current = new HashSet<Keys>(state.GetPressedKeys());

            var newlyReleased = this.pressedKeys.Except(current);
            this.UpdateReleasedKeys(newlyReleased);

            var newlyPressed = current.Except(this.pressedKeys);
            this.UpdatePressedKeys(newlyPressed);

            this.pressedKeys.Clear();
            this.pressedKeys.AddRange(current);
        }

        private void UpdatePressedKeys(IEnumerable<Keys> keys)
        {
            foreach (var key in keys)
            {
                this.Motherboard.ULA.PokeKey(key);
            }
        }

        private void UpdateReleasedKeys(IEnumerable<Keys> keys)
        {
            foreach (var key in keys)
            {
                this.Motherboard.ULA.PullKey(key);
            }
        }

        private void RunFrame() => this.Motherboard.RenderLines();

        private void DrawPixels()
        {
            this.bitmapTexture.SetData(this.Motherboard.ULA.Pixels);
            this.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
            this.spriteBatch.Draw(this.bitmapTexture, Vector2.Zero, null, Color.White, 0.0F, Vector2.Zero, DisplayScale, SpriteEffects.None, 0.0F);
            this.spriteBatch.End();
        }

        private void ChangeResolution(int width, int height)
        {
            this.graphics.PreferredBackBufferWidth = DisplayScale * width;
            this.graphics.PreferredBackBufferHeight = DisplayScale * height;
            this.graphics.ApplyChanges();
        }
    }
}
