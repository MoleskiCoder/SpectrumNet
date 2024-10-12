namespace SpectrumNet
{
    internal abstract class Joystick(Board board) : Expansion(board)
    {
        public override Type ExpansionType => Type.Joystick;

        public abstract void PushUp();

        public abstract void PushDown();

        public abstract void PushLeft();

        public abstract void PushRight();

        public abstract void PushFire();

        public abstract void ReleaseUp();

        public abstract void ReleaseDown();

        public abstract void ReleaseLeft();

        public abstract void ReleaseRight();

        public abstract void ReleaseFire();
    }
}
