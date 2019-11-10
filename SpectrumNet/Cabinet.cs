namespace SpectrumNet
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Cabinet : Game
    {
        private const int DisplayScale = 2;
        private const int DisplayWidth = Ula.RasterWidth;
        private const int DisplayHeight = Ula.RasterHeight;

        private readonly ColourPalette palette = new ColourPalette();

        private readonly List<Keys> pressed = new List<Keys>();

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
        }

        public event EventHandler<EventArgs> Initializing;

        public event EventHandler<EventArgs> Initialized;

        public Board Motherboard { get; }

        public Configuration Settings { get; }

        public void Plug(string path) => this.Motherboard.Plug(path);

        protected void OnInitializing() => this.Initializing?.Invoke(this, EventArgs.Empty);

        protected void OnInitialized() => this.Initialized?.Invoke(this, EventArgs.Empty);

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

            this.TargetElapsedTime = TimeSpan.FromSeconds(1 / Ula.FramesPerSecond);

            this.OnInitialized();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            this.CheckKeyboard();
            this.DrawFrame();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            this.DisplayTexture();
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
                    this.bitmapTexture?.Dispose();
                    this.spriteBatch?.Dispose();
                    this.graphics?.Dispose();
                }

                this.disposed = true;
            }
        }

        private void CheckKeyboard()
        {
            var state = Keyboard.GetState();
            var current = new HashSet<Keys>(state.GetPressedKeys());

            var newlyReleased = this.pressed.Except(current);
            this.UpdateReleasedKeys(newlyReleased);

            var newlyPressed = current.Except(this.pressed);
            this.UpdatePressedKeys(newlyPressed);

            this.pressed.Clear();
            this.pressed.AddRange(current);
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

        private void DrawFrame()
        {
            this.Motherboard.RunVerticalBlank();
            this.Motherboard.RunRasterLines();
            this.bitmapTexture.SetData(this.Motherboard.ULA.Pixels);
        }

        private void DisplayTexture()
        {
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
