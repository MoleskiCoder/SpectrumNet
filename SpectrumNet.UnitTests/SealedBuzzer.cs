namespace SpectrumNet.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class SealedBuzzer : AbstractBuzzer
    {
        public SealedBuzzer(int audioFrequency)
        : base(audioFrequency)
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
