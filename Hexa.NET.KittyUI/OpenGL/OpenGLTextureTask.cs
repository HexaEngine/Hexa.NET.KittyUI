namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.Utilities;
    using Hexa.NET.OpenGL;

    public unsafe struct OpenGLTextureTask
    {
        private Fence Fence;
        public OpenGLTexture2DDesc Desc;
        private uint textureId;
        private uint pboId;
        private void* mappedData;

        private GLSync syncFence;

        public readonly void* MappedData => mappedData;

        public readonly uint TextureId => textureId;

        public readonly bool Created => textureId != 0 || pboId != 0;

        /// <summary>
        /// Caller must be the creation thread.
        /// </summary>
        public void Wait()
        {
            Fence.Wait();
            Fence.Reset();
        }

        /// <summary>
        /// Caller must be the main thread.
        /// </summary>
        /// <param name="gl"></param>
        public void CreateTexture()
        {
            textureId = GL.GenTexture();
            GL.BindTexture(GLTextureTarget.Texture2D, textureId);
            // todo sampler logic
            GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)Desc.MinFilter);
            GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)Desc.MagFilter);
            GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)Desc.WrapS);
            GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)Desc.WrapT);

            GL.BindTexture(GLTextureTarget.Texture2D, 0);

            nint size = CalculatePboSize(Desc.Width, Desc.Height, Desc.MipLevels, Desc.PixelFormat, Desc.PixelType, Desc.ArraySize); // todo calculate size

            GL.CreateBuffers(1, ref pboId);
            //GL.CheckError();
            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, pboId);
            //GL.CheckError();
            GL.BufferData(GLBufferTargetARB.PixelUnpackBuffer, size, null, GLBufferUsageARB.StreamDraw);
            //GL.CheckError();
            mappedData = GL.MapBuffer(GLBufferTargetARB.PixelUnpackBuffer, GLBufferAccessARB.WriteOnly);
            //GL.CheckError();
            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);
            //GL.CheckError();

            Fence.Signal();
        }

        /// <summary>
        /// Caller must be the main thread.
        /// </summary>
        /// <param name="gl"></param>
        public void FinishTexture()
        {
            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, pboId);
            //GL.CheckError();
            GL.UnmapBuffer(GLBufferTargetARB.PixelUnpackBuffer);
            //GL.CheckError();
            syncFence = GL.FenceSync(GLSyncCondition.GpuCommandsComplete, GLSyncBehaviorFlags.None);
            //GL.CheckError();
            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);
            //GL.CheckError();
        }

        /// <summary>
        /// Polled by the main thread.
        /// </summary>
        /// <param name="gl"></param>
        public bool CheckIfDone()
        {
            if (GL.ClientWaitSync(syncFence, GLSyncObjectMask.FlushCommandsBit, 0) == GLEnum.AlreadySignaled)
            {
                GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, pboId);
                GL.BindTexture(GLTextureTarget.Texture2D, textureId);
                GL.TexImage2D(GLTextureTarget.Texture2D, 0, Desc.InternalFormat, Desc.Width, Desc.Height, 0, Desc.PixelFormat, Desc.PixelType, null);
                GL.BindTexture(GLTextureTarget.Texture2D, 0);
                GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);
                GL.DeleteSync(syncFence);
                syncFence = default;
                GL.DeleteBuffer(pboId);
                pboId = 0;
                Fence.Signal();
                return true;
            }
            return false;
        }

        private static uint GetBitsPerChannel(GLPixelType pixelType)
        {
            return pixelType switch
            {
                GLPixelType.UnsignedByte or GLPixelType.Byte => 8,
                GLPixelType.UnsignedShort or GLPixelType.Short or GLPixelType.UnsignedShort565 or GLPixelType.UnsignedShort4444 or GLPixelType.UnsignedShort5551 => 16,
                GLPixelType.UnsignedInt or GLPixelType.Int or GLPixelType.Float => 32,
                GLPixelType.HalfFloat => 16,
                // Special formats:
                GLPixelType.UnsignedByte332 => 8,// 3-3-2 bits, sum 8 bits
                GLPixelType.UnsignedByte233Rev => 8,// 2-3-3 bits, sum 8 bits (Reversed)
                GLPixelType.UnsignedShort565Rev => 16,// 5-6-5 bits, sum 16 bits (Reversed)
                GLPixelType.UnsignedShort4444Rev => 16,// 4-4-4-4 bits, sum 16 bits (Reversed)
                GLPixelType.UnsignedShort1555Rev => 16,// 1-5-5-5 bits, sum 16 bits (Reversed)
                GLPixelType.UnsignedInt8888 or GLPixelType.UnsignedInt8888Rev => 32,// 8-8-8-8 bits, sum 32 bits
                GLPixelType.UnsignedInt1010102 or GLPixelType.UnsignedInt2101010Rev => 32,// 10-10-10-2 bits, sum 32 bits
                GLPixelType.UnsignedInt248 => 32,// 24-8 bits, sum 32 bits
                GLPixelType.UnsignedInt10F11F11FRevExt => 32,// 10-11-11 floating-point bits, sum 32 bits (Reversed)
                GLPixelType.UnsignedInt5999Rev => 32,// 5-9-9-9 floating-point bits, sum 32 bits (Reversed)
                GLPixelType.Float32UnsignedInt248Rev => 64,// 32-bit float + 32-bit unsigned int (24-8 bits), sum 64 bits
                _ => throw new NotSupportedException("Unsupported pixel type."),
            };
        }

        private static uint GetChannelCount(GLPixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case GLPixelFormat.UnsignedShort:
                case GLPixelFormat.UnsignedInt:
                case GLPixelFormat.Red:
                case GLPixelFormat.Green:
                case GLPixelFormat.Blue:
                case GLPixelFormat.Alpha:
                case GLPixelFormat.RedInteger:
                case GLPixelFormat.GreenInteger:
                case GLPixelFormat.BlueInteger:
                case GLPixelFormat.StencilIndex:
                case GLPixelFormat.DepthComponent:
                    return 1;

                case GLPixelFormat.Rg:
                case GLPixelFormat.RgInteger:
                    return 2;

                case GLPixelFormat.Rgb:
                case GLPixelFormat.Bgr:
                case GLPixelFormat.RgbInteger:
                case GLPixelFormat.BgrInteger:
                case GLPixelFormat.Ycrcb422Sgix:
                case GLPixelFormat.Ycrcb444Sgix:
                    return 3;

                case GLPixelFormat.Rgba:
                case GLPixelFormat.Bgra:
                case GLPixelFormat.AbgrExt:
                case GLPixelFormat.RgbaInteger:
                case GLPixelFormat.BgraInteger:
                case GLPixelFormat.CmykExt:
                    return 4;

                case GLPixelFormat.CmykaExt:
                    return 5; // CMYK plus Alpha

                case GLPixelFormat.DepthStencil:
                    return 2; // Depth + Stencil

                default:
                    throw new NotSupportedException("Unsupported pixel format.");
            }
        }

        private static uint GetBytesPerPixel(GLPixelFormat pixelFormat, GLPixelType pixelType)
        {
            uint bitsPerChannel = GetBitsPerChannel(pixelType);
            uint channelCount = GetChannelCount(pixelFormat);

            // Für bestimmte Formate, bei denen die Bits pro Pixel festgelegt sind (nicht einfach multipliziert):
            switch (pixelType)
            {
                case GLPixelType.UnsignedByte332:
                case GLPixelType.UnsignedByte233Rev:
                    return 1; // 8 bits total
                case GLPixelType.UnsignedShort565:
                case GLPixelType.UnsignedShort565Rev:
                case GLPixelType.UnsignedShort4444:
                case GLPixelType.UnsignedShort4444Rev:
                case GLPixelType.UnsignedShort1555Rev:
                    return 2; // 16 bits total
                case GLPixelType.UnsignedInt8888:
                case GLPixelType.UnsignedInt8888Rev:
                case GLPixelType.UnsignedInt1010102:
                case GLPixelType.UnsignedInt2101010Rev:
                case GLPixelType.UnsignedInt248:
                case GLPixelType.UnsignedInt10F11F11FRevExt:
                case GLPixelType.UnsignedInt5999Rev:
                    return 4; // 32 bits total
                case GLPixelType.Float32UnsignedInt248Rev:
                    return 8; // 64 bits total
                default:
                    uint totalBitsPerPixel = bitsPerChannel * channelCount;

                    // Convert bits to bytes (8 bits = 1 byte)
                    return totalBitsPerPixel / 8;
            }
        }

        public static nint CalculatePboSize(int width, int height, uint mipLevels, GLPixelFormat pixelFormat, GLPixelType pixelType, uint arraySize = 1)
        {
            uint bytesPerPixel = GetBytesPerPixel(pixelFormat, pixelType);
            nint totalSize = 0;

            for (uint mip = 0; mip < mipLevels; mip++)
            {
                int mipWidth = Math.Max(1, width >> (int)mip);
                int mipHeight = Math.Max(1, height >> (int)mip);
                long mipSize = mipWidth * mipHeight * bytesPerPixel;
                totalSize += (nint)(mipSize * arraySize);
            }

            return totalSize;
        }
    }
}