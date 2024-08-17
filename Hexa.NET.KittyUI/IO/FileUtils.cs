namespace Hexa.NET.KittyUI.IO
{
    using System;
    using System.Buffers.Binary;
    using System.IO;

    using System.Runtime.InteropServices;

    public static class FileUtils
    {
        public static uint GetCrc32Hash(string path)
        {
            var stream = File.OpenRead(path);
            System.IO.Hashing.Crc32 crc = new();
            crc.Append(stream);
            Span<byte> buffer = stackalloc byte[4];
            crc.GetCurrentHash(buffer);
            stream.Close();

            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public static uint GetCrc32HashFromText(string text)
        {
            System.IO.Hashing.Crc32 crc = new();
            crc.Append(MemoryMarshal.AsBytes(text.AsSpan()));
            Span<byte> buffer = stackalloc byte[4];
            crc.GetCurrentHash(buffer);

            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public static FileBlob ReadBlob(string path)
        {
            FileStream fs = File.OpenRead(path);
            FileBlob blob = new((nint)fs.Length);
            fs.Read(blob.AsSpan());
            fs.Close();
            return blob;
        }
    }
}