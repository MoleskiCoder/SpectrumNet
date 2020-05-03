﻿namespace SpectrumNet
{
    using EightBit;
    using Microsoft.Xna.Framework.Audio;
    using System;

    public class Buzzer : IDisposable
    {
        private const int SampleRate = 44100;
        private const float SampleCycleRatio = SampleRate / (float)Ula.CyclesPerSecond;

        private readonly DynamicSoundEffectInstance sounds = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Mono);
        private readonly byte[] buffer;
        private int lastSample = 0;
        private short lastLevel = 0;

        private bool disposed = false;

        public Buzzer()
        {
            var numberOfSampleBytes = this.sounds.GetSampleSizeInBytes(Ula.FrameLength);
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
            for (var i = from; i < to; ++i)
            {
                this.buffer[i * 2] = EightBit.Chip.LowByte(value);
                this.buffer[(i * 2) + 1] = EightBit.Chip.HighByte(value);
            }
        }

        private static int Sample(int cycle) => (int)(cycle * SampleCycleRatio);
    }
}
