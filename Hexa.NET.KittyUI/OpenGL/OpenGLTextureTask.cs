namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using Hexa.NET.Utilities;

    /// <summary>
    /// Workflow
    /// Producer thread sends task to <see cref="UploadQueue.Enqueue(Utilities.Pointer{OpenGLTextureTask})"/>
    /// Upload thread calls <see cref="CreateTexture"/> then <see cref="Fence.Signal"/>
    /// Producer then copies to mappedData.
    /// Then Producer enqueues the task again into <see cref="UploadQueue.EnqueueFinish(Utilities.Pointer{OpenGLTextureTask})"/>
    /// Then upload thread calls <see cref="FinishTexture"/> then the task is enqueued in a internal queue to get polled <see cref="CheckIfDone"/> (polling is adaptively handled)
    /// After that upload thread calls <see cref="Fence.Signal"/> again telling the producer that everything is done.
    /// </summary>
    public unsafe struct OpenGLTextureTask
    {
        private Fence Fence;
        public OpenGLTexture2DDesc Desc;
        private uint textureId;
        private PixelUnpackBufferPoolObject pboId;
        private void* mappedData;

        private GLSync syncFence;

        public readonly void* MappedData => mappedData;

        public readonly uint TextureId => textureId;

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
        public void CreateTexture(GL GL)
        {
            textureId = OpenGLTexturePool.Global.GetNextTexture();

            Desc.PrepareTexture(GL, textureId);

            nint size = CalculatePboSize(Desc.Width, Desc.Height, Desc.MipLevels, Desc.PixelFormat, Desc.PixelType, Desc.ArraySize);

            pboId = OpenGLPixelBufferPool.Global.Rent(size);
            mappedData = pboId.Map(GL);

            Fence.Signal();
        }

        /// <summary>
        /// Caller must be the main thread.
        /// </summary>
        public void FinishTexture(GL GL)
        {
            pboId.Upload(GL, textureId, 0, 0, Desc);
            syncFence = GL.FenceSync(GLSyncCondition.GpuCommandsComplete, GLSyncBehaviorFlags.None);
        }

        /// <summary>
        /// Polled by the main thread.
        /// </summary>
        public bool CheckIfDone(GL GL, ulong timeout)
        {
            GLEnum result = GL.ClientWaitSync(syncFence, 0, timeout);
            if (result == GLEnum.ConditionSatisfied || result == GLEnum.AlreadySignaled)
            {
                OpenGLPixelBufferPool.Global.Return(pboId);
                GL.DeleteSync(syncFence);
                Fence.Signal();
                return true;
            }

            if (result == GLEnum.WaitFailed)
            {
                OpenGLPixelBufferPool.Global.Return(pboId);
                GL.DeleteSync(syncFence);
                Fence.Signal();
                OpenGLTexturePool.Global.Return(textureId);
                textureId = 0;

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