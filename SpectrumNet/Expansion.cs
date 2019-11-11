namespace SpectrumNet
{
    public abstract class Expansion : EightBit.Device
    {
        public enum Type
        {
            JOYSTICK
        }

        public Expansion(Board motherboard) => this.BUS = motherboard;

        public abstract Type ExpansionType { get; }

        protected Board BUS { get; }
    }
}
