namespace SpectrumNet
{
    using System.Collections.Generic;
    using System.IO;

    // A tape image, reduced to the sequence of data blocks the ROM
    // loader would pull off it.  A block is flag byte, data, checksum byte.
    internal abstract class TapeFile(string path)
    {
        private readonly string _path = path;
        private readonly List<byte[]> _blocks = [];
        private int _position;

        public static TapeFile Create(string path) => Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".TZX" => new TzxFile(path),
            _ => new TapFile(path),
        };

        public void Read()
        {
            this._blocks.Clear();
            this._position = 0;
            this.Parse(File.ReadAllBytes(this._path));
        }

        public bool TryNextBlock(out byte[] block)
        {
            if (this._position < this._blocks.Count)
            {
                block = this._blocks[this._position++];
                return true;
            }

            // Ran off the end of the tape: rewind, ready for another attempt
            this._position = 0;
            block = [];
            return false;
        }

        protected abstract void Parse(byte[] contents);

        protected void AddBlock(byte[] block) => this._blocks.Add(block);
    }
}
