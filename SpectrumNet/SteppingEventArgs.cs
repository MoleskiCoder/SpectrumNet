namespace SpectrumNet
{
    using System;

    internal class SteppingEventArgs(int cycles) : EventArgs
    {
        public int Cycles { get; } = cycles;
    }
}
