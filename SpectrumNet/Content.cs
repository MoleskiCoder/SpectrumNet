namespace SpectrumNet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal abstract class Content : EightBit.Rom
    {
        private ushort _position = (ushort)EightBit.Mask.Sixteen;

        public ushort Position => this._position;

        public int Remaining => this.Size - this.Position;

        public bool Finished => this.Remaining <= 0;

        public void ResetPosition() => this._position = 0;

        public void Move(ushort amount = 1) => this._position += amount;

        public void Move(int amount = 1) => this.Move((ushort)amount);

        public Span<byte> ReadBytes(ushort position, ushort amount)
        {
            Debug.Assert(position + amount <= this.Size);
            return new Span<byte>(this.Bytes(), position, amount);
        }

        public Span<byte> ReadBytes(ushort position, int amount) => this.ReadBytes(position, (ushort)amount);

        public Span<byte> FetchBytes(ushort amount)
        {
            var bytes = this.ReadBytes(this.Position, amount);
            this.Move(amount);
            return bytes;
        }

        public byte FetchByte() => this.FetchBytes(1)[0];

        public abstract EightBit.Register16 ReadShort(ushort position);
        public EightBit.Register16 ReadShort(int position) => this.ReadShort((ushort)position);

        public List<EightBit.Register16> ReadShorts(ushort position, ushort amount)
        {
            List<EightBit.Register16> returned = new(amount);
            for (ushort i = 0; i < amount; ++i)
                returned.Add(this.ReadShort(position + i * 2));
            return returned;
        }

        public List<EightBit.Register16> FetchShorts(ushort amount)
        {
            var returned = this.ReadShorts(this.Position, amount);
            this.Move(amount * sizeof(ushort));
            return returned;
        }

        public EightBit.Register16 FetchShort() => this.FetchShorts(1)[0];
    }
}
