namespace SpectrumNet
{
    using EightBit;
    using Gaming;
    using SDL3;
    using System;

    internal sealed class Buzzer : IDisposable
    {
        private const int AudioFrequency = 44100;
        private const byte LowLevel = byte.MinValue;
        private const byte HighLevel = byte.MaxValue;

        private readonly float _sampleLength;

        private readonly ScopedHandle _stream = new(SDL.DestroyAudioStream);

        private readonly byte[] _buffer;
        private int _lastSample;
        private byte _lastLevel = LowLevel;

        private bool _disposed;

        public Buzzer(float frameRate, int clockRate, SDL.AudioFormat format)
        {
            this._sampleLength = (float)AudioFrequency / (float)clockRate;
            var cyclesPerSample = (float)clockRate / (float)AudioFrequency;
            SDL.LogInfo(SDL.LogCategory.Audio, $"Audio frequency: {AudioFrequency}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"CPU Clock rate: {clockRate}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"Sample length: {this._sampleLength}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"Cycles per sample: {cyclesPerSample}");

            SDL.AudioSpec want;
            want.Freq = AudioFrequency;
            want.Format = format;
            want.Channels = 1;

            this._stream.Handle = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, want, null, IntPtr.Zero);
            Wrapper.MaybeThrowException(this._stream, "Unable to open audio stream");

            var samplesPerFrame = (float)AudioFrequency / frameRate + 1.0f;
            SDL.LogInfo(SDL.LogCategory.Audio, $"Samples per frame: {samplesPerFrame}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"Samples per frame (cast): {(ulong)samplesPerFrame}");
            this._buffer = new byte[(ulong)samplesPerFrame];

            this.Stop();
        }

        private void PlayBuffer()
        {
            this.Clear();
            var success = SDL.PutAudioStreamData(this._stream, this._buffer, this._buffer.Length);
            Wrapper.MaybeThrowException(success, "Unable to put audio data");
        }

        private void Flush()
        {
            var success = SDL.FlushAudioStream(this._stream);
            Wrapper.MaybeThrowException(success, "Unable to flush audio data");
        }

        private void Clear()
        {
            var remaining = SDL.GetAudioStreamAvailable(this._stream);
            Wrapper.MaybeThrowException(remaining != -1, "Unable to find how many audio stream bytes are available");
            if (remaining > 0)
            {
                SDL.LogWarn(SDL.LogCategory.Audio, $"Clearing {remaining} bytes of left over audio data");
                var success = SDL.ClearAudioStream(this._stream);
                Wrapper.MaybeThrowException(success, "Unable to clear audio data");
            }
        }

        public void Stop()
        {
            var success = SDL.PauseAudioStreamDevice(this._stream);
            Gaming.Wrapper.MaybeThrowException(success, "Unable to pause audio device stream");
        }

        public void Start()
        {
            var success = SDL.ResumeAudioStreamDevice(this._stream);
            Gaming.Wrapper.MaybeThrowException(success, "Unable to resume audio device stream");
        }

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
            this.FillBuffer(this._lastSample, this._buffer.Length, this._lastLevel);
            this.PlayBuffer();
            this._lastSample = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this._stream.Dispose();
                }

                this._disposed = true;
            }
        }

        private void Buzz(byte value, int sample)
        {
            this.FillBuffer(this._lastSample, sample, this._lastLevel);
            this._lastSample = sample;
            this._lastLevel = value;
        }

        private void FillBuffer(int from, int to, byte value)
        {
            var samples = this._buffer.AsSpan();
            var section = samples[from..to];
            section.Fill(value);
        }

        private int Sample(int cycle) => (int)(cycle * this._sampleLength);
    }
}
