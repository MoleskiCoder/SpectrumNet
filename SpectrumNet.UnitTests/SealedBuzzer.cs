namespace SpectrumNet.UnitTests
{
    internal class SealedBuzzer : AbstractBuzzer
    {
        public SealedBuzzer(ITimings timings, int audioFrequency)
        : base(audioFrequency, timings)
        {
        }
        protected override void PlayBuffer()
        {
        }
        protected override void Flush()
        {
        }
        protected override void Clear()
        {
        }
        protected override void Stop()
        {
        }
        protected override void Start()
        {
        }
    }
}
