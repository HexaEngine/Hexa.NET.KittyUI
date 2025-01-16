namespace Hexa.NET.KittyUI.Graphics.Imaging
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.DXGI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.OpenGL;
    using HexaGen.Runtime.COM;
    using System.IO;
    using DDSFlags = DirectXTex.DDSFlags;
    using HResult = HexaGen.Runtime.HResult;
    using ID3D11Device = NET.D3D11.ID3D11Device;
    using TGAFlags = DirectXTex.TGAFlags;
    using WICFlags = DirectXTex.WICFlags;

    public unsafe class ImageSource : IDisposable
    {
        private bool _disposed;
        private ScratchImage scImage;

        public ImageSource(ScratchImage outScImage)
        {
            scImage = outScImage;
        }

        public TexMetadata Metadata => scImage.GetMetadata();

        public int ImageCount => (int)scImage.GetImageCount();

        public void SetScratchImage(ScratchImage outScImage)
        {
            scImage.Release();
            scImage = outScImage;
        }

        public ScratchImage GetScratchImage() => scImage;

        public ImageSource Compress(ID3D11Device* device, Format format, TexCompressFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Compress4((NET.DirectXTex.ID3D11Device*)device, inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, 1, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource Compress(Format format, TexCompressFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Compress2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, 1, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource Decompress(Format format)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Decompress2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource Convert(Format format, TexFilterFlags flags, float threshold = 0.5f)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Convert2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, threshold, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource GenerateMipMaps(TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.GenerateMipMaps2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, flags, (nuint)TextureHelper.ComputeMipLevels((int)Metadata.Width, (int)Metadata.Height), ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource Resize(float scale, TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Resize2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (nuint)(Metadata.Width * scale), (nuint)(Metadata.Height * scale), flags, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource Resize(int width, int height, TexFilterFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.Resize2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (nuint)width, (nuint)height, flags, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
        }

        public ImageSource FlipRotate(TexFRFlags flags)
        {
            ScratchImage inScImage = scImage;
            ScratchImage outScImage = DirectXTex.CreateScratchImage();
            var metadata = inScImage.GetMetadata();
            DirectXTex.FlipRotate2(inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, flags, ref outScImage).ThrowIf();
            return new ImageSource(outScImage);
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
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle).ThrowIf();
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
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle).ThrowIf();
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
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, &image, 1, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle).ThrowIf();
            Texture2DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public ComPtr<ID3D11Texture3D> CreateTexture3D(ID3D11Device* device, Usage usage, BindFlag BindFlag, CpuAccessFlag accessFlags, ResourceMiscFlag miscFlag)
        {
            ComPtr<ID3D11Texture3D> resource;
            var images = scImage.GetImages();
            var nimages = scImage.GetImageCount();
            var metadata = scImage.GetMetadata();
            metadata.MiscFlags = (uint)miscFlag;
            DirectXTex.CreateTextureEx((NET.DirectXTex.ID3D11Device*)device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (NET.DirectXTex.ID3D11Resource**)&resource.Handle).ThrowIf();
            Texture3DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public uint CreateTexture2D(GLTextureWrapMode wrapS = GLTextureWrapMode.ClampToEdge, GLTextureWrapMode wrapT = GLTextureWrapMode.ClampToEdge, GLTextureMinFilter minFilter = GLTextureMinFilter.Linear, GLTextureMagFilter magFilter = GLTextureMagFilter.Linear)
        {
            var metadata = scImage.GetMetadata();

            OpenGLTextureTask* task = stackalloc OpenGLTextureTask[1];
            task->Desc = new OpenGLTexture2DDesc
            {
                Width = (int)metadata.Width,
                Height = (int)metadata.Height,
                MipLevels = (uint)metadata.MipLevels,
                ArraySize = (uint)metadata.ArraySize,
                InternalFormat = 0,
                PixelFormat = 0,
                PixelType = 0,
                WrapS = wrapS,
                WrapT = wrapT,
                MinFilter = minFilter,
                MagFilter = magFilter,
            };

            Convert((Format)metadata.Format, &task->Desc);

            OpenGLAdapter.UploadQueue.Enqueue(task);
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

        private static void Convert(Format format, OpenGLTexture2DDesc* desc)
        {
            GLInternalFormat internalFormat;
            GLPixelFormat pixelFormat = GLPixelFormat.Rgba; // Default pixel format for most cases
            GLPixelType pixelType = GLPixelType.UnsignedByte; // Default pixel type for most cases

            GLTextureSwizzle swizzleR = GLTextureSwizzle.Red;
            GLTextureSwizzle swizzleG = GLTextureSwizzle.Green;
            GLTextureSwizzle swizzleB = GLTextureSwizzle.Blue;
            GLTextureSwizzle swizzleA = GLTextureSwizzle.Alpha;

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

            if (pixelFormat == GLPixelFormat.Rgba)
            {
                pixelFormat = GLPixelFormat.Bgra;
                swizzleR = GLTextureSwizzle.Blue;
                swizzleB = GLTextureSwizzle.Red;
            }

            desc->InternalFormat = internalFormat;
            desc->PixelFormat = pixelFormat;
            desc->PixelType = pixelType;
            desc->SwizzleR = swizzleR;
            desc->SwizzleG = swizzleG;
            desc->SwizzleB = swizzleB;
            desc->SwizzleA = swizzleA;
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
                    DirectXTex.SaveToDDSFile2(image.GetImages(), image.GetImageCount(), ref meta, (DDSFlags)flags, path).ThrowIf();
                    break;

                case TexFileFormat.TGA:
                    DirectXTex.SaveToTGAFile(image.GetImages(), (TGAFlags)flags, path, ref meta).ThrowIf();
                    break;

                case TexFileFormat.HDR:
                    DirectXTex.SaveToHDRFile(image.GetImages(), path).ThrowIf();
                    break;

                default:
                    DirectXTex.SaveToWICFile2(image.GetImages(), image.GetImageCount(), (WICFlags)flags, DirectXTex.GetWICCodec(Convert(format)), path, null, default).ThrowIf();
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
                    DirectXTex.SaveToDDSMemory2(image.GetImages(), image.GetImageCount(), ref meta, (DDSFlags)flags, ref blob).ThrowIf();
                    break;

                case TexFileFormat.TGA:
                    DirectXTex.SaveToTGAMemory(image.GetImages(), (TGAFlags)flags, ref blob, ref meta).ThrowIf();
                    break;

                case TexFileFormat.HDR:
                    DirectXTex.SaveToHDRMemory(image.GetImages(), ref blob).ThrowIf();
                    break;

                default:
                    DirectXTex.SaveToWICMemory2(image.GetImages(), image.GetImageCount(), (WICFlags)flags, DirectXTex.GetWICCodec(Convert(format)), ref blob, null, default).ThrowIf();
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

        public void CopyTo(ImageSource scratchImage)
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