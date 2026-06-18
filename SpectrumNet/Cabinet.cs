namespace SpectrumNet
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    internal sealed class Cabinet : Game
    {
        private const int DisplayScale = 2;
        private const int DisplayWidth = Ula.RasterWidth;
        private const int DisplayHeight = Ula.RasterHeight;

        private readonly ColorPalette _palette = new();

        private readonly List<Keys> _pressedKeys = [];
        private readonly Dictionary<PlayerIndex, GamePadButtons> _pressedButtons = [];
        private readonly Dictionary<PlayerIndex, GamePadDPad> _pressedDPad = [];

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch? _spriteBatch;
        private Texture2D? _bitmapTexture;
        private Effect? _crtEffect;

        private bool _disposed;

        public Cabinet(Configuration configuration)
        {
            this.Settings = configuration;
            this.Motherboard = new Board(this._palette, configuration);
            this.Content.RootDirectory = "Content";

            this._graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
            };

            this._pressedButtons[PlayerIndex.One] = new GamePadButtons();
            this._pressedButtons[PlayerIndex.Two] = new GamePadButtons();
            this._pressedDPad[PlayerIndex.One] = new GamePadDPad();
            this._pressedDPad[PlayerIndex.Two] = new GamePadDPad();
        }

        public event EventHandler<EventArgs>? Initializing;

        public event EventHandler<EventArgs>? Initialized;

        public Board Motherboard { get; }

        public Configuration Settings { get; }

        public void Plug(Expansion expansion) => this.Motherboard.Plug(expansion);

        public void Plug(string path) => this.Motherboard.Plug(path);

        public void LoadSna(string path) => this.Motherboard.LoadSna(path);

        public void LoadZ80(string path) => this.Motherboard.LoadZ80(path);

        public void InsertTape(string path) => this.Motherboard.InsertTape(path);

        private void OnInitializing() => this.Initializing?.Invoke(this, EventArgs.Empty);

        private void OnInitialized() => this.Initialized?.Invoke(this, EventArgs.Empty);

        protected override void LoadContent()
        {
            base.LoadContent();
            this._crtEffect = this.Content.Load<Effect>("Shaders/crt");
            this._crtEffect.Parameters["OutputSize"]?.SetValue(new Vector2(DisplayWidth * DisplayScale, DisplayHeight * DisplayScale));
            this._crtEffect.Parameters["ScanlineStrength"]?.SetValue(0.40f);
            this._crtEffect.Parameters["PhosphorStrength"]?.SetValue(0.70f);
            this._crtEffect.Parameters["BarrelDistortion"]?.SetValue(0.12f);
            this._crtEffect.Parameters["VignetteStrength"]?.SetValue(0.30f);
        }

        protected override void Initialize()
        {
            this.OnInitializing();

            base.Initialize();

            this._spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this._bitmapTexture = new Texture2D(this.GraphicsDevice, DisplayWidth, DisplayHeight);
            this.ChangeResolution(DisplayWidth, DisplayHeight);
            this._palette.Load();

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

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            base.OnExiting(sender, args);
            this.Motherboard.LowerPOWER();
        }

        protected override void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this.Motherboard?.Dispose();
                    this._crtEffect?.Dispose();
                    this._bitmapTexture?.Dispose();
                    this._spriteBatch?.Dispose();
                    this._graphics?.Dispose();
                }

                this._disposed = true;
            }

            base.Dispose(disposing);
        }

        private void CheckGamePads() => this.MaybeHandleGamePadOne();

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
            var previousButtons = this._pressedButtons[PlayerIndex.One];

            var currentDPad = state.DPad;
            var previousDPad = this._pressedDPad[PlayerIndex.One];

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

            this._pressedButtons[PlayerIndex.One] = currentButtons;
            this._pressedDPad[PlayerIndex.One] = currentDPad;
        }

        private void CheckKeyboard()
        {
            var state = Keyboard.GetState();
            var current = new HashSet<Keys>(state.GetPressedKeys());

            var newlyReleased = this._pressedKeys.Except(current);
            this.UpdateReleasedKeys(newlyReleased);

            var newlyPressed = current.Except(this._pressedKeys);
            this.UpdatePressedKeys(newlyPressed);

            this._pressedKeys.Clear();
            this._pressedKeys.AddRange(current);
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
            Debug.Assert(this._bitmapTexture is not null);
            this._bitmapTexture.SetData(this.Motherboard.ULA.Pixels);

            var viewport = this.GraphicsDevice.Viewport;
            var matrixTransform = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1);

            Debug.Assert(this._crtEffect is not null);
            this._crtEffect.Parameters["MatrixTransform"].SetValue(matrixTransform);

            Debug.Assert(this._spriteBatch is not null);
            this._spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null, null, this._crtEffect);
            this._spriteBatch.Draw(this._bitmapTexture, Vector2.Zero, null, Color.White, 0.0F, Vector2.Zero, DisplayScale, SpriteEffects.None, 0.0F);
            this._spriteBatch.End();
        }

        private void ChangeResolution(int width, int height)
        {
            this._graphics.PreferredBackBufferWidth = DisplayScale * width;
            this._graphics.PreferredBackBufferHeight = DisplayScale * height;
            this._graphics.ApplyChanges();
        }
    }
}
