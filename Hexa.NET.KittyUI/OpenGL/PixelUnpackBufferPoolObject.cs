namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;
    using Hexa.NET.OpenGL.ARB;
    using Hexa.NET.OpenGLES.EXT;
    using System.Runtime.InteropServices;
    using GLES = OpenGLES.GL;

    public unsafe struct PixelUnpackBufferPoolObject
    {
        public uint Buffer;
        public nint Size;
        public void* MappedData;

        public void Flush()
        {
            if (GLVersion.Current.ES)
            {
                if (OpenGLAdapter.IsPersistentMappingSupported)
                {
                    GLES.FlushMappedBufferRange(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
                }
                else if (!OpenGLAdapter.NoExtensions && GLEXTMapBufferRange.TryInitExtension())
                {
                    GLEXTMapBufferRange.FlushMappedBufferRangeEXT(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
                }
                else
                {
                    GLES.UnmapBuffer(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer);
                    MappedData = null;
                }
            }
            else
            {
                if (OpenGLAdapter.IsPersistentMappingSupported)
                {
                    GL.FlushMappedBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
                }
                else if (!OpenGLAdapter.NoExtensions && GLARBMapBufferRange.TryInitExtension())
                {
                    GLARBMapBufferRange.FlushMappedBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size);
                }
                else
                {
                    GL.UnmapBuffer(GLBufferTargetARB.PixelUnpackBuffer);
                    MappedData = null;
                }
            }
        }

        public void Upload(uint textureId, int level, int slice, OpenGLTexture2DDesc desc)
        {
            bool compressed = FormatHelper.IsCompressedFormat(desc.InternalFormat);
            bool ms = desc.SampleDesc.Count > 1;
            var target = desc.TextureTarget;
            if (GLVersion.Current.ES)
            {
                GLES.BindBuffer(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, Buffer);

                GLES.BindTexture(OpenGLES.GLTextureTarget.Texture2D, textureId);

                Flush();

                GLES.TexSubImage2D(OpenGLES.GLTextureTarget.Texture2D, 0, 0, 0, desc.Width, desc.Height, (OpenGLES.GLPixelFormat)desc.PixelFormat, (OpenGLES.GLPixelType)desc.PixelType, null);

                GLES.BindBuffer(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0);

                GLES.BindTexture(OpenGLES.GLTextureTarget.Texture2D, 0);
            }
            else
            {
                GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, Buffer);

                GL.BindTexture(target, textureId);

                Flush();

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
        }

        public void* Map()
        {
            // this check prevents mapping a persistent pbo or a already mapped one.
            if (MappedData != null)
            {
                return MappedData;
            }

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, Buffer);

            if (GLVersion.Current.ES)
            {
                MappedData = GLES.MapBufferRange(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0, Size, OpenGLES.GLMapBufferAccessMask.WriteBit);
            }
            else
            {
                MappedData = GL.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, Size, GLMapBufferAccessMask.WriteBit);
            }

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);

            return MappedData;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GLCSync
    {
        [FieldOffset(0)]
        private bool es;

        [FieldOffset(1)]
        private GLSync sync;

        [FieldOffset(1)]
        private OpenGLES.GLSync syncEs;

        public readonly unsafe bool IsNull => sync.Handle == null;

        public readonly unsafe bool IsValid => sync.Handle != null;

        public GLCSync(GLSync sync)
        {
            this.sync = sync;
        }

        public GLCSync(OpenGLES.GLSync sync)
        {
            syncEs = sync;
            es = true;
        }

        public static GLCSync FenceSync(GLSyncCondition condition, GLSyncBehaviorFlags flags)
        {
            if (GLVersion.Current.ES)
            {
                return new(GLES.FenceSync((OpenGLES.GLSyncCondition)condition, (OpenGLES.GLSyncBehaviorFlags)flags));
            }
            else
            {
                return new(GL.FenceSync(condition, flags));
            }
        }

        public readonly GLEnum ClientWaitSync(GLSyncObjectMask flags, ulong timeout)
        {
            if (es)
            {
                return (GLEnum)GLES.ClientWaitSync(syncEs, (OpenGLES.GLSyncObjectMask)flags, timeout);
            }
            else
            {
                return GL.ClientWaitSync(sync, flags, timeout);
            }
        }

        public void Delete()
        {
            if (IsNull) return;
            if (es)
            {
                GLES.DeleteSync(syncEs);
            }
            else
            {
                GL.DeleteSync(sync);
            }
            this = default;
        }
    }
}