namespace SpectrumNet
{
    using EightBit;

    internal abstract class AbstractBuzzer : Device
    {
        protected readonly int _audioFrequency;
        protected readonly float _frameRate;
        protected readonly int _clockRate;

        protected float LowLevel = -0.5f;
        protected float HighLevel = +0.5f;

        protected readonly float[] _buffer;

        private int _lastSample;    // position in buffer

        protected float _lastLevel;

        protected float SampleLength => (float)this._audioFrequency / (float)this._clockRate;

        protected ulong SamplesPerFrame => (ulong)((float)this._audioFrequency / this._frameRate);

        protected AbstractBuzzer(int audioFrequency)
        : this(audioFrequency, Ula.FramesPerSecond, Ula.CpuClockRate)
        { }

        protected AbstractBuzzer(int audioFrequency, float frameRate, int clockRate)
        {
            this._lastLevel = this.LowLevel;

            this._audioFrequency = audioFrequency;
            this._frameRate = frameRate;
            this._clockRate = clockRate;

            this._buffer = new float[this.SamplesPerFrame];
        }

        public override void RaisePOWER()
        {
            base.RaisePOWER();
            this.Initialise();
        }

        public override void LowerPOWER()
        {
            this.Terminate();
            base.LowerPOWER();
        }

        public virtual void Initialise() => this.Start();

        public virtual void Terminate() => this.Stop();

        protected abstract void PlayBuffer();

        protected abstract void Flush();

        protected abstract void Clear();

        protected abstract void Stop();

        protected abstract void Start();

        public void Buzz(PinLevel state, int cycle)
        {
            var level = state.Raised() ? HighLevel : LowLevel;
            this.Buzz(level, Sample(cycle));
        }

        public void EndFrame()
        {
            this.FillBuffer(this._lastSample, this._buffer.Length, this._lastLevel);
            this.PlayBuffer();
            this._lastSample = 0;
        }

        private void Buzz(float value, int sample)
        {
            this.FillBuffer(this._lastSample, sample, this._lastLevel);
            this._lastSample = sample;
            this._lastLevel = value;
        }

        private void FillBuffer(int from, int to, float value)
        {
            var samples = this._buffer.AsSpan();
            var section = samples[from..to];
            section.Fill(value);
        }

        private int Sample(int cycle) => (int)(cycle * this.SampleLength);
    }
}
