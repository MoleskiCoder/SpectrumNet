namespace SpectrumNet
{
    using System;
    using System.IO;

    // TZX tape image: a signed container of typed blocks.  Only the blocks
    // carrying ROM-loadable data (standard and turbo speed) matter to the
    // flash loader; everything else describes signal timing or metadata
    // and is skipped.
    internal sealed class TzxFile(string path) : TapeFile(path)
    {
        private static readonly byte[] _signature = [(byte)'Z', (byte)'X', (byte)'T', (byte)'a', (byte)'p', (byte)'e', (byte)'!', 0x1a];

        private byte[] _contents = [];
        private int _offset;

        protected override void Parse(byte[] contents)
        {
            this._contents = contents;
            this._offset = 0;

            if (!this.Take(_signature.Length).AsSpan().SequenceEqual(_signature))
            {
                throw new InvalidDataException("Missing TZX signature");
            }

            this.Skip(2);   // Major and minor version

            while (this._offset < this._contents.Length)
            {
                this.ParseBlock();
            }
        }

        private void ParseBlock()
        {
            var id = this.TakeByte();
            switch (id)
            {
                case 0x10:  // Standard speed data
                    this.Skip(2);   // Pause after block
                    this.AddBlock(this.Take(this.TakeWord()));
                    break;

                case 0x11:  // Turbo speed data
                    this.Skip(15);  // Pulse timings, pilot count, used bits, pause
                    this.AddBlock(this.Take(this.TakeTriple()));
                    break;

                case 0x12:  // Pure tone
                    this.Skip(4);
                    break;

                case 0x13:  // Pulse sequence
                    this.Skip(this.TakeByte() * 2);
                    break;

                case 0x14:  // Pure data: raw bits for custom loaders, not LD-BYTES
                    this.Skip(7);   // Pulse timings, used bits, pause
                    this.Skip(this.TakeTriple());
                    break;

                case 0x15:  // Direct recording
                    this.Skip(5);   // Sample rate, pause, used bits
                    this.Skip(this.TakeTriple());
                    break;

                case 0x18:  // CSW recording
                case 0x19:  // Generalized data
                case 0x2b:  // Set signal level
                    this.Skip(this.TakeDoubleWord());
                    break;

                case 0x20:  // Pause (or stop the tape)
                case 0x23:  // Jump to block
                case 0x24:  // Loop start
                    this.Skip(2);
                    break;

                case 0x21:  // Group start
                case 0x30:  // Text description
                    this.Skip(this.TakeByte());
                    break;

                case 0x22:  // Group end
                case 0x25:  // Loop end
                case 0x27:  // Return from sequence
                    break;

                case 0x26:  // Call sequence
                    this.Skip(this.TakeWord() * 2);
                    break;

                case 0x28:  // Select block
                case 0x32:  // Archive info
                    this.Skip(this.TakeWord());
                    break;

                case 0x2a:  // Stop the tape if in 48K mode
                    this.Skip(4);
                    break;

                case 0x31:  // Message
                    this.Skip(1);   // Display time
                    this.Skip(this.TakeByte());
                    break;

                case 0x33:  // Hardware type
                    this.Skip(this.TakeByte() * 3);
                    break;

                case 0x35:  // Custom info
                    this.Skip(16);  // Identification string
                    this.Skip(this.TakeDoubleWord());
                    break;

                case 0x5a:  // Glue (concatenated TZX files)
                    this.Skip(9);
                    break;

                default:
                    throw new InvalidDataException($"Unsupported TZX block type: {id:x2}");
            }
        }

        private void Skip(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
            ArgumentOutOfRangeException.ThrowIfLessThan(this._contents.Length - this._offset, count, nameof(count));
            this._offset += count;
        }

        private byte[] Take(int count)
        {
            var from = this._offset;
            this.Skip(count);
            return this._contents[from..this._offset];
        }

        private byte TakeByte()
        {
            this.Skip(1);
            return this._contents[this._offset - 1];
        }

        private int TakeWord() => this.TakeByte() | (this.TakeByte() << 8);

        private int TakeTriple() => this.TakeWord() | (this.TakeByte() << 16);

        private int TakeDoubleWord() => this.TakeWord() | (this.TakeWord() << 16);
    }
}
