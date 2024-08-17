namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.Utilities;
    using Silk.NET.OpenGL;

    public unsafe struct OpenGLTextureTask
    {
        private Fence Fence;
        public OpenGLTexture2DDesc Desc;
        private uint textureId;
        private uint pboId;
        private void* mappedData;

        private nint syncFence;

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
        public void CreateTexture(GL gl)
        {
            gl.GenTextures(1, out textureId);
            gl.BindTexture(TextureTarget.Texture2D, textureId);
            // todo sampler logic
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)Desc.MinFilter);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)Desc.MagFilter);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)Desc.WrapS);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)Desc.WrapT);

            gl.BindTexture(TextureTarget.Texture2D, 0);

            nuint size = CalculatePboSize(Desc.Width, Desc.Height, Desc.MipLevels, Desc.PixelFormat, Desc.PixelType, Desc.ArraySize); // todo calculate size

            gl.CreateBuffers(1, out pboId);
            gl.CheckError();
            gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pboId);
            gl.CheckError();
            gl.BufferData(BufferTargetARB.PixelUnpackBuffer, size, null, (GLEnum)BufferUsageARB.StreamDraw);
            gl.CheckError();
            mappedData = gl.MapBuffer(BufferTargetARB.PixelUnpackBuffer, BufferAccessARB.WriteOnly);
            gl.CheckError();
            gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
            gl.CheckError();

            Fence.Signal();
        }

        /// <summary>
        /// Caller must be the main thread.
        /// </summary>
        /// <param name="gl"></param>
        public void FinishTexture(GL gl)
        {
            gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pboId);
            gl.CheckError();
            gl.UnmapBuffer(BufferTargetARB.PixelUnpackBuffer);
            gl.CheckError();
            syncFence = gl.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
            gl.CheckError();
            gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);

            gl.CheckError();
        }

        /// <summary>
        /// Polled by the main thread.
        /// </summary>
        /// <param name="gl"></param>
        public bool CheckIfDone(GL gl)
        {
            if (gl.ClientWaitSync(syncFence, SyncObjectMask.Bit, 0) == GLEnum.AlreadySignaled)
            {
                gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, pboId);
                gl.BindTexture(TextureTarget.Texture2D, textureId);
                gl.TexImage2D(TextureTarget.Texture2D, 0, (int)Desc.InternalFormat, Desc.Width, Desc.Height, 0, Desc.PixelFormat, Desc.PixelType, null);
                gl.BindTexture(TextureTarget.Texture2D, 0);
                gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
                gl.DeleteSync(syncFence);
                syncFence = 0;
                gl.DeleteBuffer(pboId);
                pboId = 0;
                Fence.Signal();
                return true;
            }
            return false;
        }

        private static uint GetBitsPerChannel(PixelType pixelType)
        {
            return pixelType switch
            {
                PixelType.UnsignedByte or PixelType.Byte => 8,
                PixelType.UnsignedShort or PixelType.Short or PixelType.UnsignedShort565 or PixelType.UnsignedShort4444 or PixelType.UnsignedShort5551 => 16,
                PixelType.UnsignedInt or PixelType.Int or PixelType.Float => 32,
                PixelType.HalfFloat => 16,
                // Special formats:
                PixelType.UnsignedByte332 => 8,// 3-3-2 bits, sum 8 bits
                PixelType.UnsignedByte233Rev => 8,// 2-3-3 bits, sum 8 bits (Reversed)
                PixelType.UnsignedShort565Rev => 16,// 5-6-5 bits, sum 16 bits (Reversed)
                PixelType.UnsignedShort4444Rev => 16,// 4-4-4-4 bits, sum 16 bits (Reversed)
                PixelType.UnsignedShort1555Rev => 16,// 1-5-5-5 bits, sum 16 bits (Reversed)
                PixelType.UnsignedInt8888 or PixelType.UnsignedInt8888Rev => 32,// 8-8-8-8 bits, sum 32 bits
                PixelType.UnsignedInt1010102 or PixelType.UnsignedInt2101010Rev => 32,// 10-10-10-2 bits, sum 32 bits
                PixelType.UnsignedInt248 => 32,// 24-8 bits, sum 32 bits
                PixelType.UnsignedInt10f11f11fRev => 32,// 10-11-11 floating-point bits, sum 32 bits (Reversed)
                PixelType.UnsignedInt5999Rev => 32,// 5-9-9-9 floating-point bits, sum 32 bits (Reversed)
                PixelType.Float32UnsignedInt248Rev => 64,// 32-bit float + 32-bit unsigned int (24-8 bits), sum 64 bits
                _ => throw new NotSupportedException("Unsupported pixel type."),
            };
        }

        private static uint GetChannelCount(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.UnsignedShort:
                case PixelFormat.UnsignedInt:
                case PixelFormat.Red:
                case PixelFormat.Green:
                case PixelFormat.Blue:
                case PixelFormat.Alpha:
                case PixelFormat.RedInteger:
                case PixelFormat.GreenInteger:
                case PixelFormat.BlueInteger:
                case PixelFormat.StencilIndex:
                case PixelFormat.DepthComponent:
                    return 1;

                case PixelFormat.RG:
                case PixelFormat.RGInteger:
                    return 2;

                case PixelFormat.Rgb:
                case PixelFormat.Bgr:
                case PixelFormat.RgbInteger:
                case PixelFormat.BgrInteger:
                case PixelFormat.Ycrcb422Sgix:
                case PixelFormat.Ycrcb444Sgix:
                    return 3;

                case PixelFormat.Rgba:
                case PixelFormat.Bgra:
                case PixelFormat.AbgrExt:
                case PixelFormat.RgbaInteger:
                case PixelFormat.BgraInteger:
                case PixelFormat.CmykExt:
                    return 4;

                case PixelFormat.CmykaExt:
                    return 5; // CMYK plus Alpha

                case PixelFormat.DepthStencil:
                    return 2; // Depth + Stencil

                default:
                    throw new NotSupportedException("Unsupported pixel format.");
            }
        }

        private static uint GetBytesPerPixel(PixelFormat pixelFormat, PixelType pixelType)
        {
            uint bitsPerChannel = GetBitsPerChannel(pixelType);
            uint channelCount = GetChannelCount(pixelFormat);

            // Für bestimmte Formate, bei denen die Bits pro Pixel festgelegt sind (nicht einfach multipliziert):
            switch (pixelType)
            {
                case PixelType.UnsignedByte332:
                case PixelType.UnsignedByte233Rev:
                    return 1; // 8 bits total
                case PixelType.UnsignedShort565:
                case PixelType.UnsignedShort565Rev:
                case PixelType.UnsignedShort4444:
                case PixelType.UnsignedShort4444Rev:
                case PixelType.UnsignedShort1555Rev:
                    return 2; // 16 bits total
                case PixelType.UnsignedInt8888:
                case PixelType.UnsignedInt8888Rev:
                case PixelType.UnsignedInt1010102:
                case PixelType.UnsignedInt2101010Rev:
                case PixelType.UnsignedInt248:
                case PixelType.UnsignedInt10f11f11fRev:
                case PixelType.UnsignedInt5999Rev:
                    return 4; // 32 bits total
                case PixelType.Float32UnsignedInt248Rev:
                    return 8; // 64 bits total
                default:
                    uint totalBitsPerPixel = bitsPerChannel * channelCount;

                    // Convert bits to bytes (8 bits = 1 byte)
                    return totalBitsPerPixel / 8;
            }
        }

        public static nuint CalculatePboSize(uint width, uint height, uint mipLevels, PixelFormat pixelFormat, PixelType pixelType, uint arraySize = 1)
        {
            uint bytesPerPixel = GetBytesPerPixel(pixelFormat, pixelType);
            nuint totalSize = 0;

            for (uint mip = 0; mip < mipLevels; mip++)
            {
                uint mipWidth = Math.Max(1, width >> (int)mip);
                uint mipHeight = Math.Max(1, height >> (int)mip);
                nuint mipSize = mipWidth * mipHeight * bytesPerPixel;
                totalSize += mipSize * arraySize;
            }

            return totalSize;
        }
    }
}