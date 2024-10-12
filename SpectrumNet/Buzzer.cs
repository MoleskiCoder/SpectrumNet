namespace SpectrumNet
{
    using EightBit;
    using Microsoft.Xna.Framework.Audio;
    using System;
    using System.Runtime.InteropServices;

    internal class Buzzer : IDisposable
    {
        private const int SampleRate = 44100;
        private const float SampleCycleRatio = SampleRate / (float)Ula.CyclesPerSecond;

        private readonly DynamicSoundEffectInstance sounds = new(SampleRate, AudioChannels.Mono);
        private readonly byte[] buffer;
        private int lastSample;
        private short lastLevel;

        private bool disposed;

        public Buzzer()
        {
            var numberOfSampleBytes = this.sounds.GetSampleSizeInBytes(Ula.FrameLength);
            if (numberOfSampleBytes % 2 != 0)
            {
                ++numberOfSampleBytes;
            }
            this.buffer = new byte[numberOfSampleBytes];
            this.sounds.Play();
        }
        private int NumberOfSamples => this.buffer.Length / 2;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Buzz(EightBit.PinLevel state, int cycle)
        {
            var level = state.Raised() ? short.MaxValue : short.MinValue;
            this.Buzz(level, Sample(cycle));
        }

        public void EndFrame()
        {
            this.FillBuffer(this.lastSample, this.NumberOfSamples, this.lastLevel);
            this.sounds.SubmitBuffer(this.buffer);
            this.lastSample = 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.sounds.Dispose();
                }

                this.disposed = true;
            }
        }

        private void Buzz(short value, int sample)
        {
            this.FillBuffer(this.lastSample, sample, this.lastLevel);
            this.lastSample = sample;
            this.lastLevel = value;
        }

        private void FillBuffer(int from, int to, short value)
        {
            var samples = MemoryMarshal.Cast<byte, short>(this.buffer);
            var section = samples[from..to];
            section.Fill(value);
        }

        private static int Sample(int cycle) => (int)(cycle * SampleCycleRatio);
    }
}
