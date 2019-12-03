namespace SpectrumNet
{
    using System;

    public class SteppingEventArgs : EventArgs
    {
        public SteppingEventArgs(int cycles) => this.Cycles = cycles;

        public int Cycles { get; } = 0;
    }
}
