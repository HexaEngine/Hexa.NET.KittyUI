namespace Hexa.NET.Kitty.Graphics.Imaging
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.Kitty.D3D11;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.OpenGL;
    using System.IO;
    using DDSFlags = DirectXTex.DDSFlags;
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

    public unsafe class D3DScratchImage
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
            DirectXTex.Compress4(device, inScImage.GetImages(), inScImage.GetImageCount(), ref metadata, (int)format, flags, 1, ref outScImage);
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
            DirectXTex.CreateTextureEx(device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (ID3D11Resource**)&resource.Handle);
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
            DirectXTex.CreateTextureEx(device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (ID3D11Resource**)&resource.Handle);
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
            DirectXTex.CreateTextureEx(device, &image, 1, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (ID3D11Resource**)&resource.Handle);
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
            DirectXTex.CreateTextureEx(device, images, nimages, ref metadata, (int)usage, (uint)BindFlag, (uint)accessFlags, (uint)miscFlag, CreateTexFlags.Default, (ID3D11Resource**)&resource.Handle);
            Texture3DDesc desc = default;
            resource.GetDesc(ref desc);

            return resource;
        }

        public uint CreateTexture1D(GL gl, TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureMinFilter minFilter = TextureMinFilter.LinearMipmapLinear, TextureMagFilter magFilter = TextureMagFilter.Linear)
        {
            // Generate a texture ID for OpenGL
            gl.GenTextures(1, out uint _textureID);

            var metadata = scImage.GetMetadata();
            TextureTarget textureTarget = metadata.ArraySize > 1 ? TextureTarget.Texture1DArray : TextureTarget.Texture1D;

            // Bind the texture
            gl.BindTexture(textureTarget, _textureID);

            // Set wrapping mode
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapS, (int)wrapS);

            // Set filtering modes
            gl.TexParameter(textureTarget, TextureParameterName.TextureMinFilter, (int)minFilter);
            gl.TexParameter(textureTarget, TextureParameterName.TextureMagFilter, (int)magFilter);

            var (internalFormat, pixelFormat, pixelType) = Convert((Format)metadata.Format);
            var compressed = DirectXTex.IsCompressed(metadata.Format);

            // Upload the image data to OpenGL
            for (ulong mip = 0; mip < metadata.MipLevels; mip++)
            {
                for (uint item = 0; item < metadata.ArraySize; item++)
                {
                    var image = scImage.GetImage(mip, item, 0);

                    if (compressed)
                    {
                        // Compressed formats
                        gl.CompressedTexImage1D(
                            textureTarget,
                            (int)mip,
                            (GLEnum)internalFormat,
                            (uint)image->Width,
                            0,
                            (uint)image->SlicePitch,
                            image->Pixels
                        );
                    }
                    else
                    {
                        // Uncompressed formats
                        gl.TexImage1D(
                            textureTarget,
                            (int)mip,
                            (int)internalFormat,
                            (uint)image->Width,
                            0,
                            (GLEnum)pixelFormat,
                            (GLEnum)pixelType,
                            image->Pixels
                        );
                    }
                }
            }

            gl.BindTexture(textureTarget, 0);

            return _textureID;
        }

        public uint CreateTexture2D(GL gl, TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat, TextureMinFilter minFilter = TextureMinFilter.LinearMipmapLinear, TextureMagFilter magFilter = TextureMagFilter.Linear)
        {
            // Generate a texture ID for OpenGL
            gl.GenTextures(1, out uint _textureID);

            // Bind the texture
            var metadata = scImage.GetMetadata();
            TextureTarget textureTarget = TextureTarget.Texture2D;
            if (metadata.ArraySize > 1)
            {
                textureTarget = TextureTarget.Texture2DArray;
                if (metadata.ArraySize % 6 == 0 && metadata.IsCubemap())
                {
                    textureTarget = TextureTarget.TextureCubeMap;
                    if (metadata.ArraySize > 6)
                    {
                        textureTarget = TextureTarget.TextureCubeMapArray;
                    }
                }
            }

            gl.BindTexture(textureTarget, _textureID);

            // Set wrapping modes
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapS, (int)wrapS);
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapT, (int)wrapT);

            if (textureTarget == TextureTarget.TextureCubeMap)
            {
                // For cubemaps, set the R wrap mode as well
                gl.TexParameter(textureTarget, TextureParameterName.TextureWrapR, (int)wrapS);
            }

            // Set filtering modes
            gl.TexParameter(textureTarget, TextureParameterName.TextureMinFilter, (int)minFilter);
            gl.TexParameter(textureTarget, TextureParameterName.TextureMagFilter, (int)magFilter);

            // Determine the appropriate OpenGL internal format, pixel format, and type based on the DXGI format
            var (internalFormat, pixelFormat, pixelType) = Convert((Format)metadata.Format);
            var compressed = DirectXTex.IsCompressed(metadata.Format);

            // Upload the image data to OpenGL
            for (ulong mip = 0; mip < metadata.MipLevels; mip++)
            {
                for (uint item = 0; item < metadata.ArraySize; item++)
                {
                    var image = scImage.GetImage(mip, item, 0);

                    if (compressed)
                    {
                        // Compressed formats
                        gl.CompressedTexImage2D(
                            GetTargetForCubeMap(textureTarget, item),
                            (int)mip,
                            (GLEnum)internalFormat,
                            (uint)image->Width,
                            (uint)image->Height,
                            0,
                            (uint)image->SlicePitch,
                            image->Pixels
                        );
                    }
                    else
                    {
                        // Uncompressed formats
                        gl.TexImage2D(
                            GetTargetForCubeMap(textureTarget, item),
                            (int)mip,
                            (int)internalFormat,
                            (uint)image->Width,
                            (uint)image->Height,
                            0,
                            (GLEnum)pixelFormat,
                            (GLEnum)pixelType,
                            image->Pixels
                        );
                    }
                }
            }

            gl.BindTexture(textureTarget, 0);

            return _textureID;
        }

        public uint CreateTexture3D(GL gl, TextureWrapMode wrapS = TextureWrapMode.Repeat, TextureWrapMode wrapT = TextureWrapMode.Repeat, TextureWrapMode wrapR = TextureWrapMode.Repeat, TextureMinFilter minFilter = TextureMinFilter.LinearMipmapLinear, TextureMagFilter magFilter = TextureMagFilter.Linear)
        {
            // Generate a texture ID for OpenGL
            gl.GenTextures(1, out uint _textureID);

            var metadata = scImage.GetMetadata();
            TextureTarget textureTarget = TextureTarget.Texture3D;

            // Bind the texture
            gl.BindTexture(textureTarget, _textureID);

            // Set wrapping modes
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapS, (int)wrapS);
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapT, (int)wrapT);
            gl.TexParameter(textureTarget, TextureParameterName.TextureWrapR, (int)wrapR);

            // Set filtering modes
            gl.TexParameter(textureTarget, TextureParameterName.TextureMinFilter, (int)minFilter);
            gl.TexParameter(textureTarget, TextureParameterName.TextureMagFilter, (int)magFilter);

            var (internalFormat, pixelFormat, pixelType) = Convert((Format)metadata.Format);
            var compressed = DirectXTex.IsCompressed(metadata.Format);

            // Upload the image data to OpenGL
            for (ulong mip = 0; mip < metadata.MipLevels; mip++)
            {
                var image = scImage.GetImage(mip, 0, 0);  // No need for item or slice for depth in 3D texture
                if (compressed)
                {
                    // Compressed formats
                    gl.CompressedTexImage3D(
                        textureTarget,
                        (int)mip,
                        (GLEnum)internalFormat,
                        (uint)image->Width,
                        (uint)image->Height,
                        (uint)metadata.Depth,  // Use metadata.Depth instead of image->Depth
                        0,
                        (uint)image->SlicePitch,
                        image->Pixels
                    );
                }
                else
                {
                    // Uncompressed formats
                    gl.TexImage3D(
                        textureTarget,
                        (int)mip,
                        (int)internalFormat,
                        (uint)image->Width,
                        (uint)image->Height,
                        (uint)metadata.Depth,  // Use metadata.Depth instead of image->Depth
                        0,
                        (GLEnum)pixelFormat,
                        (GLEnum)pixelType,
                        image->Pixels
                    );
                }
            }

            gl.BindTexture(textureTarget, 0);

            return _textureID;
        }

        private TextureTarget GetTargetForCubeMap(TextureTarget textureTarget, uint item)
        {
            // If this is a cubemap, return the correct face target
            if (textureTarget == TextureTarget.TextureCubeMap)
            {
                return (TextureTarget)((uint)TextureTarget.TextureCubeMapPositiveX + item);
            }
            // Otherwise, return the original texture target (for 2D textures or arrays)
            return textureTarget;
        }

        private static (InternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType) Convert(Format format)
        {
            InternalFormat internalFormat;
            PixelFormat pixelFormat = PixelFormat.Rgba; // Default pixel format for most cases
            PixelType pixelType = PixelType.UnsignedByte; // Default pixel type for most cases

            switch (format)
            {
                case Format.FormatR8G8B8A8Unorm:
                    internalFormat = InternalFormat.Rgba8;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.UnsignedByte;
                    break;

                case Format.FormatR8G8B8A8SNorm:
                    internalFormat = InternalFormat.Rgba8SNorm;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.Byte;
                    break;

                case Format.FormatR32G32B32A32Float:
                    internalFormat = InternalFormat.Rgba32f;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.Float;
                    break;

                case Format.FormatR16G16B16A16Float:
                    internalFormat = InternalFormat.Rgba16f;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.HalfFloat;
                    break;

                case Format.FormatBC1Unorm:
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt1Ext;
                    break;

                case Format.FormatBC2Unorm:
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt3Ext;
                    break;

                case Format.FormatBC3Unorm:
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt5Ext;
                    break;

                case Format.FormatBC4Unorm:
                    internalFormat = InternalFormat.CompressedRedRgtc1;
                    break;

                case Format.FormatBC5Unorm:
                    internalFormat = InternalFormat.CompressedRGRgtc2;
                    break;

                case Format.FormatBC6HUF16:
                    internalFormat = InternalFormat.CompressedRgbBptcUnsignedFloat;
                    break;

                case Format.FormatBC7Unorm:
                    internalFormat = InternalFormat.CompressedRgbaBptcUnorm;
                    break;

                case Format.FormatR8Unorm:
                    internalFormat = InternalFormat.R8;
                    pixelFormat = PixelFormat.Red;
                    pixelType = PixelType.UnsignedByte;
                    break;

                case Format.FormatR16Float:
                    internalFormat = InternalFormat.R16f;
                    pixelFormat = PixelFormat.Red;
                    pixelType = PixelType.HalfFloat;
                    break;

                case Format.FormatR32Float:
                    internalFormat = InternalFormat.R32f;
                    pixelFormat = PixelFormat.Red;
                    pixelType = PixelType.Float;
                    break;

                case Format.FormatR16G16Float:
                    internalFormat = InternalFormat.RG16f;
                    pixelFormat = PixelFormat.RG;
                    pixelType = PixelType.HalfFloat;
                    break;

                case Format.FormatR32G32Float:
                    internalFormat = InternalFormat.RG32f;
                    pixelFormat = PixelFormat.RG;
                    pixelType = PixelType.Float;
                    break;

                case Format.FormatR10G10B10A2Unorm:
                    internalFormat = InternalFormat.Rgb10A2;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.UnsignedInt1010102;
                    break;

                case Format.FormatR11G11B10Float:
                    internalFormat = InternalFormat.R11fG11fB10f;
                    pixelFormat = PixelFormat.Rgb;
                    pixelType = PixelType.UnsignedInt10f11f11fRev;
                    break;

                case Format.FormatR16G16B16A16Unorm:
                    internalFormat = InternalFormat.Rgba16;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.UnsignedShort;
                    break;

                case Format.FormatR16G16B16A16SNorm:
                    internalFormat = InternalFormat.Rgba16SNorm;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.Short;
                    break;

                case Format.FormatR16G16B16A16Uint:
                    internalFormat = InternalFormat.Rgba16ui;
                    pixelFormat = PixelFormat.RgbaInteger;
                    pixelType = PixelType.UnsignedShort;
                    break;

                case Format.FormatR16G16B16A16Sint:
                    internalFormat = InternalFormat.Rgba16i;
                    pixelFormat = PixelFormat.RgbaInteger;
                    pixelType = PixelType.Short;
                    break;

                case Format.FormatR32G32B32A32Uint:
                    internalFormat = InternalFormat.Rgba32ui;
                    pixelFormat = PixelFormat.RgbaInteger;
                    pixelType = PixelType.UnsignedInt;
                    break;

                case Format.FormatR32G32B32A32Sint:
                    internalFormat = InternalFormat.Rgba32i;
                    pixelFormat = PixelFormat.RgbaInteger;
                    pixelType = PixelType.Int;
                    break;

                // Add additional DXGI format mappings as needed...

                default:
                    throw new NotSupportedException($"DXGI format {(Format)format} is not supported.");
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