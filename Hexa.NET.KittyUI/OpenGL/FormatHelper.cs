namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using System;

    public class FormatHelper
    {
        public static bool IsCompressedFormat(GLInternalFormat format)
        {
            return format == GLInternalFormat.CompressedRgbaS3TcDxt1Ext ||
                   format == GLInternalFormat.CompressedRgbaS3TcDxt3Ext ||
                   format == GLInternalFormat.CompressedRgbaS3TcDxt5Ext ||
                   format == GLInternalFormat.CompressedRgbaBptcUnorm ||
                   format == GLInternalFormat.CompressedRgbBptcSignedFloat ||
                   format == GLInternalFormat.CompressedRgbBptcUnsignedFloat;
        }

        public static int CalculateCompressedDataSize(GLInternalFormat format, int width, int height)
        {
            // Determine block size for each format
            int blockSize;
            if (format == GLInternalFormat.CompressedRgbaS3TcDxt1Ext)
            {
                blockSize = 8; // DXT1
            }
            else if (format == GLInternalFormat.CompressedRgbaS3TcDxt3Ext || format == GLInternalFormat.CompressedRgbaS3TcDxt5Ext)
            {
                blockSize = 16; // DXT3/DXT5
            }
            else if (format == GLInternalFormat.CompressedRgbaBptcUnorm || format == GLInternalFormat.CompressedRgbBptcUnsignedFloat)
            {
                blockSize = 16; // BC7
            }
            else if (format == GLInternalFormat.CompressedRgbBptcSignedFloat)
            {
                blockSize = 16; // BC6H
            }
            else
            {
                throw new ArgumentException("Unsupported compressed format");
            }

            // Calculate the number of blocks
            int blocksWide = Math.Max(1, (width + 3) / 4);
            int blocksHigh = Math.Max(1, (height + 3) / 4);

            // Return the total data size for the specified mip level
            return blocksWide * blocksHigh * blockSize;
        }
    }
}