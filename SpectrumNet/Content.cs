namespace SpectrumNet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal abstract class Content : EightBit.Rom
    {
        private ushort _position = (ushort)EightBit.Mask.Sixteen;
        private bool _locked;

        public ushort Position => this._position;

        public int Remaining => this.Size - this.Position;

        public bool Finished => this.Remaining <= 0;

        public bool Locked => this._locked;
        public bool Unlocked => !this._locked;

        public void ResetPosition() => this._position = 0;

        public void Lock(bool locking = true) => this._locked = locking;
        public void Unlock() => this.Lock(false);

        public void Move(ushort amount = 1)
        {
            Debug.Assert(this.Unlocked);
            this._position += amount;
        }

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

        public Span<byte> ReadBytes() => this.ReadBytes(0, this.Size);

        public byte ReadByte(ushort position) => this.ReadBytes(position, 1)[0];

        public byte FetchByte() => this.FetchBytes(1)[0];

        public abstract EightBit.Register16 ReadWord(ushort position);
        public EightBit.Register16 ReadWord(int position) => this.ReadWord((ushort)position);

        public List<EightBit.Register16> ReadWords(ushort position, ushort amount)
        {
            List<EightBit.Register16> returned = new(amount);
            for (ushort i = 0; i < amount; ++i)
                returned.Add(this.ReadWord(position + i * 2));
            return returned;
        }

        public List<EightBit.Register16> FetchWords(ushort amount)
        {
            var returned = this.ReadWords(this.Position, amount);
            this.Move(amount * sizeof(ushort));
            return returned;
        }

        public EightBit.Register16 FetchWord() => this.FetchWords(1)[0];
    }
}
