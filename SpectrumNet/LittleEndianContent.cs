namespace SpectrumNet
{
    internal class LittleEndianContent : Content
    {
        public override EightBit.Register16 ReadShort(ushort position)
        {
            var bytes = this.ReadBytes(position, 2);
            return new EightBit.Register16(bytes[0], bytes[1]);
        }
    }
}
