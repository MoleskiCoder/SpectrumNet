namespace SpectrumNet
{
    using Gaming;
    using SDL3;
    using System;

    internal sealed class Buzzer(SDL.AudioFormat format = SDL.AudioFormat.AudioU8) : AbstractBuzzer<byte>(AudioFrequency)
    {
        private const int AudioFrequency = 44100;

        private readonly SDL.AudioFormat _format = format;

        private readonly ScopedHandle _stream = new(SDL.DestroyAudioStream);

        public override void Initialise()
        {
            SDL.LogInfo(SDL.LogCategory.Audio, $"Audio frequency: {this._audioFrequency}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"CPU Clock rate: {this._clockRate}");
            SDL.LogInfo(SDL.LogCategory.Audio, $"Sample length: {this.SampleLength}");

            SDL.AudioSpec want;
            want.Freq = AudioFrequency;
            want.Format = this._format;
            want.Channels = 1;

            this._stream.Handle = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, want, null, IntPtr.Zero);
            Wrapper.MaybeThrowException(this._stream, "Unable to open audio stream");

            SDL.LogInfo(SDL.LogCategory.Audio, $"Samples per frame: {this.SamplesPerFrame}");

            base.Initialise();
        }

        public override void Terminate()
        {
            base.Terminate();
            this._stream.Dispose();
        }

        protected override void PlayBuffer()
        {
            this.Clear();   // Avoid audio "drift"
            var success = SDL.PutAudioStreamData(this._stream, this._buffer, this._buffer.Length);
            Wrapper.MaybeThrowException(success, "Unable to put audio data");
        }

        protected override void Flush()
        {
            var success = SDL.FlushAudioStream(this._stream);
            Wrapper.MaybeThrowException(success, "Unable to flush audio data");
        }

        protected override void Clear()
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

        protected override void Stop()
        {
            var success = SDL.PauseAudioStreamDevice(this._stream);
            Gaming.Wrapper.MaybeThrowException(success, "Unable to pause audio device stream");
        }

        protected override void Start()
        {
            var success = SDL.ResumeAudioStreamDevice(this._stream);
            Gaming.Wrapper.MaybeThrowException(success, "Unable to resume audio device stream");
        }
    }
}
