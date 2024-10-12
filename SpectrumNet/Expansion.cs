namespace SpectrumNet
{
    internal abstract class Expansion(Board motherboard) : EightBit.Device
    {
        internal enum Type
        {
            Joystick
        }

        public abstract Type ExpansionType { get; }

        protected Board BUS { get; } = motherboard;
    }
}
