namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;
    using Hexa.NET.OpenGLES.EXT;

#else

    using Hexa.NET.OpenGL;
    using Hexa.NET.OpenGL.ARB;

#endif

    using System.Runtime.InteropServices;

    public unsafe struct PixelUnpackBufferPoolObject
    {
        public uint Buffer;
        public nint Size;
        public void* MappedData;

        public void Flush(GL GL)
        {
            if (OpenGLAdapter.IsPersistentMappingSupported)
            {
                GL.FlushMappedBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
            }
#if GLES
            else if (!OpenGLAdapter.NoExtensions && GL.TryGetExtension<GLEXTMapBufferRange>(out var GLEXTMapBufferRange))
            {
                GLEXTMapBufferRange.FlushMappedBufferRangeEXT(GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
            }
#else
            else if (!OpenGLAdapter.NoExtensions && GL.TryGetExtension<GLARBMapBufferRange>(out var GLARBMapBufferRange))
            {
                GLARBMapBufferRange.FlushMappedBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
            }
#endif
            else
            {
                GL.UnmapBuffer(GLBufferTargetARB.PixelUnpackBuffer);
                MappedData = null;
            }
        }

        public void Upload(GL GL, uint textureId, int level, int slice, OpenGLTexture2DDesc desc)
        {
            bool compressed = FormatHelper.IsCompressedFormat(desc.InternalFormat);
            bool ms = desc.SampleDesc.Count > 1;
            var target = desc.TextureTarget;

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, Buffer);

            GL.BindTexture(target, textureId);

            Flush(GL);

            if (ms)
            {
            }
            else
            {
                int mipWidth = Math.Max(1, desc.Width >> level);
                int mipHeight = Math.Max(1, desc.Height >> level);
                int compressedSize = compressed ? FormatHelper.CalculateCompressedDataSize(desc.InternalFormat, mipWidth, mipHeight) : 0;

                switch (target)
                {
                    case GLTextureTarget.Texture2D:
                        if (compressed)
                        {
                            GL.CompressedTexSubImage2D(target, level, 0, 0, mipWidth, mipHeight, desc.InternalFormat, compressedSize, null);
                        }
                        else
                        {
                            GL.TexSubImage2D(target, level, 0, 0, mipWidth, mipHeight, desc.PixelFormat, desc.PixelType, null);
                        }
                        break;

                    case GLTextureTarget.Texture2DArray or GLTextureTarget.CubeMapArray:
                        if (compressed)
                        {
                            GL.CompressedTexSubImage3D(target, level, 0, 0, slice, mipWidth, mipHeight, 1, desc.InternalFormat, compressedSize, null);
                        }
                        else
                        {
                            GL.TexSubImage3D(target, level, 0, 0, slice, mipWidth, mipHeight, 1, desc.PixelFormat, desc.PixelType, null);
                        }
                        break;

                    case GLTextureTarget.CubeMap:
                        GLTextureTarget i = (GLTextureTarget)((int)GLTextureTarget.CubeMapPositiveX + slice);
                        if (compressed)
                        {
                            GL.CompressedTexSubImage2D(i, level, 0, 0, mipWidth, mipHeight, desc.InternalFormat, compressedSize, null);
                        }
                        else
                        {
                            GL.TexSubImage2D(i, level, 0, 0, mipWidth, mipHeight, desc.PixelFormat, desc.PixelType, null);
                        }
                        break;
                }
            }

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);

            GL.BindTexture(target, 0);
        }

        public void* Map(GL GL)
        {
            // this check prevents mapping a persistent pbo or a already mapped one.
            if (MappedData != null)
            {
                return MappedData;
            }

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, Buffer);

            MappedData = GL.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size, GLMapBufferAccessMask.WriteBit | GLMapBufferAccessMask.UnsynchronizedBit);

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);

            return MappedData;
        }
    }
}