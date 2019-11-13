namespace SpectrumNet
{
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
            var numberOfSampleBytes = this.sounds.GetSampleSizeInBytes(Ula.FrameLength);
            this.buffer = new byte[numberOfSampleBytes];
            this.sounds.Play();
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
            this.lastSample = 0;
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
