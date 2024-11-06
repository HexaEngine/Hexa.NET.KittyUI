namespace Hexa.NET.KittyUI.Graphics.Imaging
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.DXGI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.OpenGL;
    using Hexa.NET.OpenGL;
    using HexaGen.Runtime.COM;
    using System.IO;
    using DDSFlags = DirectXTex.DDSFlags;
    using HResult = HexaGen.Runtime.HResult;
    using ID3D11Device = NET.D3D11.ID3D11Device;
    using TGAFlags = DirectXTex.TGAFlags;
    using WICFlags = DirectXTex.WICFlags;

    public enum TexFileFormat
    {
        Auto,
        DDS,
        TGA,
        HDR,
        BMP,
        JPEG,
        PNG,
        TIFF,
        GIF,
        WMP,
        ICO,
    }

    public unsafe class D3DScratchImage : IDisposable
    {
        private bool _disposed;
        private ScratchImage scImage;

        public D3DScratchImage(ScratchImage outScImage)
        {
            scImage = outScImage;
        }

        public TexMetadata Metadata => scImage.GetMetadata();

        public int ImageCount => (int)scImage.GetImageCount();

        public D3DScratchImage Compress(ID3D11Device* device, Format format, TexCompressFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Compress4((NET.DirectXTex.ID3D11Device*)device, inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, 1, ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public D3DScratchImage Decompress(Format format)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Decompress2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public D3DScratchImage Convert(Format format, TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Convert2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, 0, ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public D3DScratchImage GenerateMipMaps(TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.GenerateMipMaps2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, flags, (nuint)TextureHelper.ComputeMipLevels((int)Metadata.Width, (int)Metadata.Height), ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public D3DScratchImage Resize(float scale, TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Resize2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (nuint)(Metadata.Width * scale), (nuint)(Metadata.Height * scale), flags, ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public D3DScratchImage Resize(int width, int height, TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Resize2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (nuint)width, (nuint)height, flags, ref outScImage);
            return new D3DScratchImage(outScImage);
        }

        public bool OverwriteFormat(Format format)
        {
            return scImage.OverrideFormat((int)format);
        }

        public ComPtr<ID3D11Texture1D> CreateTexture1D(ID3D11Device* device, Usage usage, BindFlag BindFlag, CpuAccessFlag accessFlags, ResourceMiscFlag miscFlag)
        {
            ComPtr<ID3D11Texture1D> resource;
            var images = scImage.GetImages();
            var nimages = scImage.GetImageCount();
            var metadata = scImage.GetMetadata();
            metadata.MiscFlags = (uint)miscFlag;
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle);
            Texture1DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public ComPtr<ID3D11Texture2D> CreateTexture2D(ID3D11Device* device, Usage usage, BindFlag BindFlag, CpuAccessFlag accessFlags, ResourceMiscFlag miscFlag)
        {
            ComPtr<ID3D11Texture2D> resource;
            var images = scImage.GetImages();
            var nimages = scImage.GetImageCount();
            var metadata = scImage.GetMetadata();
            metadata.MiscFlags = (uint)miscFlag;
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle);
            Texture2DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public ComPtr<ID3D11Texture2D> CreateTexture2D(ID3D11Device* device, int index, Usage usage, BindFlag BindFlag, CpuAccessFlag accessFlags, ResourceMiscFlag miscFlag)
        {
            ComPtr<ID3D11Texture2D> resource;
            var images = scImage.GetImages();
            var metadata = scImage.GetMetadata();
            var image = images[index];
            metadata.Width = image.Width;
            metadata.Height = image.Height;
            metadata.ArraySize = 1;
            metadata.MipLevels = 1;
            metadata.MiscFlags = (uint)miscFlag;
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, &image, 1, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle);
            Texture2DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public ComPtr<ID3D11Texture3D> CreateTexture3D(ID3D11Device* device, Usage usage, BindFlag BindFlag, CpuAccessFlag accessFlags, ResourceMiscFlag miscFlag)
        {
            ScratchImage inScImage = scImage;
            ComPtr<ID3D11Texture3D> resource;
            var images = scImage.GetImages();
            var nimages = scImage.GetImageCount();
            var metadata = scImage.GetMetadata();
            metadata.MiscFlags = (uint)miscFlag;
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle);
            Texture3DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public uint CreateTexture2D(GLTextureWrapMode wrapS = GLTextureWrapMode.ClampToEdge, GLTextureWrapMode wrapT = GLTextureWrapMode.ClampToEdge, GLTextureMinFilter minFilter = GLTextureMinFilter.Linear, GLTextureMagFilter magFilter = GLTextureMagFilter.Linear)
        {
            var metadata = scImage.GetMetadata();
            var (internalFormat, pixelFormat, pixelType) = Convert((Format)metadata.Format);

            OpenGLTextureTask* task = stackalloc OpenGLTextureTask[1];
            task->Desc = new OpenGLTexture2DDesc
            {
                Width = (int)metadata.Width,
                Height = (int)metadata.Height,
                MipLevels = (uint)metadata.MipLevels,
                ArraySize = (uint)metadata.ArraySize,
                InternalFormat = internalFormat,
                PixelFormat = pixelFormat,
                PixelType = pixelType,
                WrapS = wrapS,
                WrapT = wrapT,
                MinFilter = minFilter,
                MagFilter = magFilter,
            };

            if (OpenGLAdapter.UploadQueue.Enqueue(task)) // handle async texture loading.
            {
                task->Wait();

                var pData = (byte*)task->MappedData;

                if (pData == null)
                {
                    throw new InvalidOperationException("Failed to map the texture data.");
                }

                // Do uploading
                for (uint mip = 0; mip < metadata.MipLevels; mip++)
                {
                    for (uint item = 0; item < metadata.ArraySize; item++)
                    {
                        var image = scImage.GetImage(mip, item, 0);

                        Memcpy(image->Pixels, pData, image->SlicePitch, image->SlicePitch);

                        pData += image->SlicePitch;
                    }
                }

                OpenGLAdapter.UploadQueue.EnqueueFinish(task);

                task->Wait();

                return task->TextureId;
            }

            // Generate a texture ID for OpenGL
            uint _textureID = OpenGLTexturePool.Global.GetNextTexture();

            GLTextureTarget textureTarget = GLTextureTarget.Texture2D;
            if (metadata.ArraySize > 1)
            {
                textureTarget = GLTextureTarget.Texture2DArray;
                if (metadata.ArraySize % 6 == 0 && metadata.IsCubemap())
                {
                    textureTarget = GLTextureTarget.CubeMap;
                    if (metadata.ArraySize > 6)
                    {
                        textureTarget = GLTextureTarget.CubeMapArray;
                    }
                }
            }

            GL.BindTexture(textureTarget, _textureID);

            // Set wrapping modes
            GL.TexParameteri(textureTarget, GLTextureParameterName.WrapS, (int)wrapS);
            GL.TexParameteri(textureTarget, GLTextureParameterName.WrapT, (int)wrapT);

            if (textureTarget == GLTextureTarget.CubeMap)
            {
                // For cubemaps, set the R wrap mode as well
                GL.TexParameteri(textureTarget, GLTextureParameterName.WrapR, (int)wrapS);
            }

            // Set filtering modes
            GL.TexParameteri(textureTarget, GLTextureParameterName.MinFilter, (int)minFilter);
            GL.TexParameteri(textureTarget, GLTextureParameterName.MagFilter, (int)magFilter);

            // Determine the appropriate OpenGL internal format, pixel format, and type based on the DXGI format
            var compressed = DirectXTex.IsCompressed(metadata.Format);

            // Upload the image data to OpenGL
            for (uint mip = 0; mip < metadata.MipLevels; mip++)
            {
                for (uint item = 0; item < metadata.ArraySize; item++)
                {
                    var image = scImage.GetImage(mip, item, 0);

                    if (compressed)
                    {
                        // Compressed formats
                        GL.CompressedTexImage2D(
                            GetTargetForCubeMap(textureTarget, item),
                            (int)mip,
                            internalFormat,
                            (int)image->Width,
                            (int)image->Height,
                            0,
                            (int)image->SlicePitch,
                            image->Pixels
                        );
                    }
                    else
                    {
                        // Uncompressed formats
                        GL.TexImage2D(
                            GetTargetForCubeMap(textureTarget, item),
                            (int)mip,
                            internalFormat,
                            (int)image->Width,
                            (int)image->Height,
                            0,
                            pixelFormat,
                            pixelType,
                            image->Pixels
                        );
                    }
                }
            }

            GL.BindTexture(textureTarget, 0);

            return _textureID;
        }

        private GLTextureTarget GetTargetForCubeMap(GLTextureTarget textureTarget, uint item)
        {
            // If this is a cubemap, return the correct face target
            if (textureTarget == GLTextureTarget.CubeMap)
            {
                return (GLTextureTarget)((uint)GLTextureTarget.CubeMapPositiveX + item);
            }
            // Otherwise, return the original texture target (for 2D textures or arrays)
            return textureTarget;
        }

        private static (GLInternalFormat internalFormat, GLPixelFormat pixelFormat, GLPixelType pixelType) Convert(Format format)
        {
            GLInternalFormat internalFormat;
            GLPixelFormat pixelFormat = GLPixelFormat.Rgba; // Default pixel format for most cases
            GLPixelType pixelType = GLPixelType.UnsignedByte; // Default pixel type for most cases

            switch (format)
            {
                case Format.R8G8B8A8Unorm:
                    internalFormat = GLInternalFormat.Rgba8;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.UnsignedByte;
                    break;

                case Format.R8G8B8A8Snorm:
                    internalFormat = GLInternalFormat.Rgba8Snorm;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.Byte;
                    break;

                case Format.R32G32B32A32Float:
                    internalFormat = GLInternalFormat.Rgba32F;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.Float;
                    break;

                case Format.R16G16B16A16Float:
                    internalFormat = GLInternalFormat.Rgba16F;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.HalfFloat;
                    break;

                case Format.Bc1Unorm:
                    internalFormat = GLInternalFormat.CompressedRgbaS3TcDxt1Ext;
                    break;

                case Format.Bc2Unorm:
                    internalFormat = GLInternalFormat.CompressedRgbaS3TcDxt3Ext;
                    break;

                case Format.Bc3Unorm:
                    internalFormat = GLInternalFormat.CompressedRgbaS3TcDxt5Ext;
                    break;

                case Format.Bc4Unorm:
                    internalFormat = GLInternalFormat.CompressedRedRgtc1;
                    break;

                case Format.Bc5Unorm:
                    internalFormat = GLInternalFormat.CompressedRgRgtc2;
                    break;

                case Format.Bc6HUf16:
                    internalFormat = GLInternalFormat.CompressedRgbBptcUnsignedFloat;
                    break;

                case Format.Bc7Unorm:
                    internalFormat = GLInternalFormat.CompressedRgbaBptcUnorm;
                    break;

                case Format.R8Unorm:
                    internalFormat = GLInternalFormat.R8;
                    pixelFormat = GLPixelFormat.Red;
                    pixelType = GLPixelType.UnsignedByte;
                    break;

                case Format.R16Float:
                    internalFormat = GLInternalFormat.R16F;
                    pixelFormat = GLPixelFormat.Red;
                    pixelType = GLPixelType.HalfFloat;
                    break;

                case Format.R32Float:
                    internalFormat = GLInternalFormat.R32F;
                    pixelFormat = GLPixelFormat.Red;
                    pixelType = GLPixelType.Float;
                    break;

                case Format.R16G16Float:
                    internalFormat = GLInternalFormat.Rg16F;
                    pixelFormat = GLPixelFormat.Rg;
                    pixelType = GLPixelType.HalfFloat;
                    break;

                case Format.R32G32Float:
                    internalFormat = GLInternalFormat.Rg32F;
                    pixelFormat = GLPixelFormat.Rg;
                    pixelType = GLPixelType.Float;
                    break;

                case Format.R10G10B10A2Unorm:
                    internalFormat = GLInternalFormat.Rgb10A2;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.UnsignedInt1010102;
                    break;

                case Format.R11G11B10Float:
                    internalFormat = GLInternalFormat.R11FG11FB10F;
                    pixelFormat = GLPixelFormat.Rgb;
                    pixelType = GLPixelType.UnsignedInt10F11F11FRevExt;
                    break;

                case Format.R16G16B16A16Unorm:
                    internalFormat = GLInternalFormat.Rgba16;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.UnsignedShort;
                    break;

                case Format.R16G16B16A16Snorm:
                    internalFormat = GLInternalFormat.Rgba16Snorm;
                    pixelFormat = GLPixelFormat.Rgba;
                    pixelType = GLPixelType.Short;
                    break;

                case Format.R16G16B16A16Uint:
                    internalFormat = GLInternalFormat.Rgba16Ui;
                    pixelFormat = GLPixelFormat.RgbaInteger;
                    pixelType = GLPixelType.UnsignedShort;
                    break;

                case Format.R16G16B16A16Sint:
                    internalFormat = GLInternalFormat.Rgba16I;
                    pixelFormat = GLPixelFormat.RgbaInteger;
                    pixelType = GLPixelType.Short;
                    break;

                case Format.R32G32B32A32Uint:
                    internalFormat = GLInternalFormat.Rgba32Ui;
                    pixelFormat = GLPixelFormat.RgbaInteger;
                    pixelType = GLPixelType.UnsignedInt;
                    break;

                case Format.R32G32B32A32Sint:
                    internalFormat = GLInternalFormat.Rgba32I;
                    pixelFormat = GLPixelFormat.RgbaInteger;
                    pixelType = GLPixelType.Int;
                    break;

                case Format.B8G8R8A8Unorm:
                    internalFormat = GLInternalFormat.Rgba;
                    pixelFormat = GLPixelFormat.Bgra;
                    pixelType = GLPixelType.UnsignedByte;
                    break;

                // Add additional DXGI format mappings as needed...

                default:
                    throw new NotSupportedException($"DXGI format {format} is not supported.");
            }

            return (internalFormat, pixelFormat, pixelType);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                scImage.Release();
                scImage = default;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void SaveToFile(string path, TexFileFormat format, int flags)
        {
            ScratchImage image = scImage;
            var meta = image.GetMetadata();
            switch (format)
            {
                case TexFileFormat.Auto:
                    var ext = Path.GetExtension(path);
                    if (ext == ".dds")
                    {
                        SaveToFile(path, TexFileFormat.DDS, flags);
                    }
                    else if (ext == ".tga")
                    {
                        SaveToFile(path, TexFileFormat.TGA, flags);
                    }
                    else if (ext == ".hdr")
                    {
                        SaveToFile(path, TexFileFormat.HDR, flags);
                    }
                    else if (ext == ".bmp")
                    {
                        SaveToFile(path, TexFileFormat.BMP, flags);
                    }
                    else if (ext == ".jpg")
                    {
                        SaveToFile(path, TexFileFormat.JPEG, flags);
                    }
                    else if (ext == ".jpeg")
                    {
                        SaveToFile(path, TexFileFormat.JPEG, flags);
                    }
                    else if (ext == ".png")
                    {
                        SaveToFile(path, TexFileFormat.PNG, flags);
                    }
                    else if (ext == ".tiff")
                    {
                        SaveToFile(path, TexFileFormat.TIFF, flags);
                    }
                    else if (ext == ".gif")
                    {
                        SaveToFile(path, TexFileFormat.GIF, flags);
                    }
                    else if (ext == ".wmp")
                    {
                        SaveToFile(path, TexFileFormat.WMP, flags);
                    }
                    else if (ext == ".ico")
                    {
                        SaveToFile(path, TexFileFormat.ICO, flags);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    break;

                case TexFileFormat.DDS:
                    DirectXTex.SaveToDDSFile2(image.GetImages(), image.GetImageCount(), ref meta, (DDSFlags)flags, path);
                    break;

                case TexFileFormat.TGA:
                    DirectXTex.SaveToTGAFile(image.GetImages(), (TGAFlags)flags, path, ref meta);
                    break;

                case TexFileFormat.HDR:
                    DirectXTex.SaveToHDRFile(image.GetImages(), path);
                    break;

                default:
                    DirectXTex.SaveToWICFile2(image.GetImages(), image.GetImageCount(), (WICFlags)flags, DirectXTex.GetWICCodec(Convert(format)), path, null, default);
                    break;
            }
        }

        public void SaveToMemory(Stream stream, TexFileFormat format, int flags)
        {
            ScratchImage image = scImage;
            Blob blob = DirectXTex.CreateBlob();

            var meta = image.GetMetadata();
            switch (format)
            {
                case TexFileFormat.DDS:
                    DirectXTex.SaveToDDSMemory2(image.GetImages(), image.GetImageCount(), ref meta, (DDSFlags)flags, ref blob);
                    break;

                case TexFileFormat.TGA:
                    DirectXTex.SaveToTGAMemory(image.GetImages(), (TGAFlags)flags, ref blob, ref meta);
                    break;

                case TexFileFormat.HDR:
                    DirectXTex.SaveToHDRMemory(image.GetImages(), ref blob);
                    break;

                default:
                    DirectXTex.SaveToWICMemory2(image.GetImages(), image.GetImageCount(), (WICFlags)flags, DirectXTex.GetWICCodec(Convert(format)), ref blob, null, default);
                    break;
            }

            Span<byte> buffer = new(blob.GetBufferPointer(), (int)blob.GetBufferSize());
            stream.Write(buffer);
            blob.Release();
        }

        public static WICCodecs Convert(TexFileFormat format)
        {
            return format switch
            {
                TexFileFormat.DDS => throw new NotSupportedException(),
                TexFileFormat.TGA => throw new NotSupportedException(),
                TexFileFormat.HDR => throw new NotSupportedException(),
                TexFileFormat.BMP => WICCodecs.CodecBmp,
                TexFileFormat.JPEG => WICCodecs.CodecJpeg,
                TexFileFormat.PNG => WICCodecs.CodecPng,
                TexFileFormat.TIFF => WICCodecs.CodecTiff,
                TexFileFormat.GIF => WICCodecs.CodecGif,
                TexFileFormat.WMP => WICCodecs.CodecWmp,
                TexFileFormat.ICO => WICCodecs.CodecIco,
                _ => throw new NotSupportedException(),
            };
        }

        public Image[] GetImages()
        {
            Image[] images = new Image[ImageCount];
            var pImages = scImage.GetImages();
            for (int i = 0; i < images.Length; i++)
            {
                images[i] = pImages[i];
            }
            return images;
        }

        private static void Swap<T>(ref ScratchImage target, T context, Func<T, ScratchImage, ScratchImage, HResult> callback)
        {
            ScratchImage scratchImage = DirectXTex.CreateScratchImage();
            HResult hr = callback(context, target, scratchImage);
            if (!hr.IsSuccess)
            {
                scratchImage.Release(); // Release the scratch image if the operation failed, to avoid memory leaks
                hr.Throw();
            }
            target.Release();
            target = scratchImage;
        }

        public void CopyTo(D3DScratchImage scratchImage)
        {
            var a = this;
            var b = scratchImage;
            var metaA = a.Metadata;
            var metaB = b.Metadata;
            if (metaA.Format != metaB.Format)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"Format {metaA.Format} doesn't match with {metaB.Format}");
            }

            if (metaA.Width != metaB.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"Width {metaA.Width} doesn't match with {metaB.Width}");
            }

            if (metaA.Height != metaB.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"Height {metaA.Height} doesn't match with {metaB.Height}");
            }

            if (metaA.Depth != metaB.Depth)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"Depth {metaA.Depth} doesn't match with {metaB.Depth}");
            }

            if (metaA.ArraySize != metaB.ArraySize)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"ArraySize {metaA.ArraySize} doesn't match with {metaB.ArraySize}");
            }

            if (metaA.MipLevels != metaB.MipLevels)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"MipLevels {metaA.MipLevels} doesn't match with {metaB.MipLevels}");
            }

            if (metaA.Dimension != metaB.Dimension)
            {
                throw new ArgumentOutOfRangeException(nameof(scratchImage), $"Dimension {metaA.Dimension} doesn't match with {metaB.Dimension}");
            }

            var count = a.ImageCount;
            var aImages = a.GetImages();
            var bImages = b.GetImages();
            for (int i = 0; i < count; i++)
            {
                var aImage = aImages[i];
                var bImage = bImages[i];
                Memcpy(aImage.Pixels, bImage.Pixels, aImage.RowPitch * aImage.Height, bImage.RowPitch * bImage.Height);
            }
        }
    }
}