namespace SpectrumNet
{
    using EightBit;
    using Microsoft.Xna.Framework.Audio;
    using System;
    using System.Runtime.InteropServices;

    internal class Buzzer : IDisposable
    {
        private const int AudioFrequency = 44100;
        private const short LowLevel = short.MinValue;
        private const short HighLevel = short.MaxValue;

        private const float SampleLength = AudioFrequency / (float)Ula.CpuClockRate;

        private readonly DynamicSoundEffectInstance _sounds = new(AudioFrequency, AudioChannels.Mono);
        private readonly byte[] _buffer;
        private int _lastSample;
        private short _lastLevel = LowLevel;

        private bool _disposed;

        public Buzzer()
        {
            var numberOfSampleBytes = this._sounds.GetSampleSizeInBytes(Ula.FrameLength);
            if (numberOfSampleBytes % 2 != 0)
            {
                ++numberOfSampleBytes;
            }
            this._buffer = new byte[numberOfSampleBytes];
            this._sounds.Play();
        }

        private int NumberOfSamples => this._buffer.Length / 2;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Buzz(EightBit.PinLevel state, int cycle)
        {
            var level = state.Raised() ? HighLevel : LowLevel;
            this.Buzz(level, Sample(cycle));
        }

        public void EndFrame()
        {
            this.FillBuffer(this._lastSample, this.NumberOfSamples, this._lastLevel);
            this._sounds.SubmitBuffer(this._buffer);
            this._lastSample = 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this._sounds.Dispose();
                }

                this._disposed = true;
            }
        }

        private void Buzz(short value, int sample)
        {
            this.FillBuffer(this._lastSample, sample, this._lastLevel);
            this._lastSample = sample;
            this._lastLevel = value;
        }

        private void FillBuffer(int from, int to, short value)
        {
            var samples = MemoryMarshal.Cast<byte, short>(this._buffer.AsSpan());
            var section = samples[from..to];
            section.Fill(value);
        }

        private static int Sample(int cycle) => (int)(cycle * SampleLength);
    }
}
