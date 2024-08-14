namespace Hexa.NET.Kitty.IO
{
    public class Crc32
    {
        private static readonly uint[] crcTable;

        static Crc32()
        {
            uint c;
            if (crcTable == null)
            {
                crcTable = new uint[256];
                for (uint n = 0; n <= 255; n++)
                {
                    c = n;
                    for (var k = 0; k <= 7; k++)
                    {
                        if ((c & 1) == 1)
                            c = 0xEDB88320 ^ c >> 1 & 0x7FFFFFFF;
                        else
                            c = c >> 1 & 0x7FFFFFFF;
                    }
                    crcTable[n] = c;
                }
            }
        }

        public static uint HashToUInt32(ReadOnlySpan<byte> span, uint crc)
        {
            uint c = crc ^ 0xffffffff;
            var endOffset = span.Length;
            for (var i = 0; i < endOffset; i++)
            {
                c = crcTable[(c ^ span[i]) & 0xff] ^ c >> 8;
            }
            return c ^ 0xffffffff;
        }
    }
}