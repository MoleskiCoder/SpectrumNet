namespace SpectrumNet
{
    using System;
    using EightBit;
    using Microsoft.Xna.Framework.Audio;

    public class Buzzer
    {
        private const int SampleRate = 44100;

        private readonly DynamicSoundEffectInstance sounds = new DynamicSoundEffectInstance(SampleRate, AudioChannels.Mono);
        private readonly byte[] buffer;
        private int lastSample = 0;
        private short lastLevel = 0;

        public Buzzer()
        {
            var frameLength = TimeSpan.FromSeconds(1.0 / Ula.FramesPerSecond);
            var numberOfSampleBytes = this.sounds.GetSampleSizeInBytes(frameLength);
            this.buffer = new byte[numberOfSampleBytes];
        }

        private int NumberOfSamples => this.buffer.Length / 2;

        public void Buzz(EightBit.PinLevel state, int cycle)
        {
            var level = state.Raised() ? short.MaxValue : short.MinValue;
            this.Buzz(level, Sample(cycle));
        }

        public void EndFrame()
        {
            this.FillBuffer(this.lastSample, this.NumberOfSamples, this.lastLevel);
            this.sounds.SubmitBuffer(this.buffer);
            this.sounds.Play();
            this.lastSample = 0;
        }

        private void Buzz(short value, int sample)
        {
            if (sample < this.lastSample)
            {
                throw new InvalidOperationException("Whoops: this sample comes before last sample!");
            }

            this.FillBuffer(this.lastSample, sample, this.lastLevel);
            this.lastSample = sample;
            this.lastLevel = value;
        }

        private void FillBuffer(int from, int to, short value)
        {
            for (var i = from; i < to; ++i)
            {
                var low = EightBit.Chip.LowByte(value);
                var high = EightBit.Chip.HighByte(value);
                this.buffer[i * 2] = low;
                this.buffer[(i * 2) + 1] = high;
            }
        }

        private static int Sample(int cycle)
        {
            var ratio = (float)SampleRate / (float)Ula.CyclesPerSecond;
            var sample = (float)cycle * ratio;
	        return (int)sample;
        }
    }
}
