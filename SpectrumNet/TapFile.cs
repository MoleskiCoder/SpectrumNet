namespace SpectrumNet
{
    using System.IO;

    // TAP tape image: a sequence of blocks, each preceded by a two byte
    // little-endian length.
    internal sealed class TapFile(string path) : TapeFile(path)
    {
        protected override void Parse(byte[] contents)
        {
            var offset = 0;
            while (offset < contents.Length)
            {
                if ((contents.Length - offset) < 2)
                {
                    throw new InvalidDataException("Truncated TAP block length");
                }

                var length = EightBit.Chip.MakeShort(contents[offset], contents[offset + 1]);
                offset += 2;

                if ((contents.Length - offset) < length)
                {
                    throw new InvalidDataException("Truncated TAP block data");
                }

                this.AddBlock(contents[offset..(offset + length)]);
                offset += length;
            }
        }
    }
}
