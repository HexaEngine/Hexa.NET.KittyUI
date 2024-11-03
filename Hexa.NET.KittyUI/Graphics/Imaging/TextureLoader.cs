namespace Hexa.NET.KittyUI.Graphics.Imaging
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.DXGI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.StbImage;
    using HexaGen.Runtime;
    using HexaGen.Runtime.COM;
    using System.IO;
    using ID3D11DeviceContext = NET.D3D11.ID3D11DeviceContext;
    using ID3D11Resource = NET.D3D11.ID3D11Resource;

    public enum TextureLoaderFlags
    {
        None = 0,
        GenerateMipMaps = 1,
        Scale = 2
    }

    public static class Extensions
    {
        public static void ThrowIf(this int hresult)
        {
            HResult result = hresult;
            result.ThrowIf();
        }
    }

    public unsafe class TextureLoader
    {
        public TextureLoader()
        {
        }

        public static bool ForceNonWIC { get; set; }

        public static D3DScratchImage CaptureTexture(ID3D11DeviceContext* context, ID3D11Resource* resource)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.CaptureTexture((NET.DirectXTex.ID3D11Device*)D3D11GraphicsDevice.Device.Handle, (NET.DirectXTex.ID3D11DeviceContext*)context, (NET.DirectXTex.ID3D11Resource*)resource, ref image).ThrowIf();
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage LoadFormFile(string filename)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            ReadOnlySpan<char> extension = Path.GetExtension(filename.AsSpan());
            try
            {
                switch (extension)
                {
                    case ".dds":
                        DirectXTex.LoadFromDDSFile(filename, DDSFlags.None, null, ref image).ThrowIf();
                        break;

                    case ".tga":
                        DirectXTex.LoadFromTGAFile(filename, TGAFlags.None, null, ref image).ThrowIf();
                        break;

                    case ".hdr":
                        DirectXTex.LoadFromHDRFile(filename, null, ref image).ThrowIf();
                        break;

                    default:
                        if (OperatingSystem.IsWindows() && !ForceNonWIC)
                        {
                            DirectXTex.LoadFromWICFile(filename, WICFlags.None, null, ref image, default).ThrowIf();
                        }
                        else
                        {
                            HandleStbImageFile(filename, ref image).ThrowIf();
                        }
                        break;
                };
            }
            catch (Exception)
            {
                image.Release(); // avoid memory leak if exception is thrown
                throw;
            }

            return new D3DScratchImage(image);
        }

        private static HResult HandleStbImageFile(string filename, ref ScratchImage image)
        {
            if (StbImage.Is16Bit(filename) == 1)
            {
                return HandleStbImage16File(filename, ref image);
            }
            int width, height, channels;
            byte* imgData = StbImage.Load(filename, &width, &height, &channels, 0);

            return LoadStbImage(ref image, width, height, channels, imgData);
        }

        private static HResult HandleStbImage16File(string filename, ref ScratchImage image)
        {
            int width, height, channels;
            ushort* imgData = StbImage.Load16(filename, &width, &height, &channels, 0);

            return LoadStbImage16(ref image, width, height, channels, imgData);
        }

        public static D3DScratchImage LoadFromMemory(ImageFileFormat format, ReadOnlySpan<byte> data)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            try
            {
                fixed (byte* pData = data)
                {
                    switch (format)
                    {
                        case ImageFileFormat.DDS:
                            DirectXTex.LoadFromDDSMemory(pData, (nuint)data.Length, DDSFlags.None, null, ref image).ThrowIf();
                            break;

                        case ImageFileFormat.TGA:
                            DirectXTex.LoadFromTGAMemory(pData, (nuint)data.Length, TGAFlags.None, null, ref image).ThrowIf();
                            break;

                        case ImageFileFormat.HDR:
                            DirectXTex.LoadFromHDRMemory(pData, (nuint)data.Length, null, ref image).ThrowIf();
                            break;

                        default:
                            if (OperatingSystem.IsWindows() && !ForceNonWIC)
                            {
                                DirectXTex.LoadFromWICMemory(pData, (nuint)data.Length, WICFlags.None, null, ref image, default).ThrowIf();
                            }
                            else
                            {
                                HandleStbImageMemory(format, pData, data.Length, ref image).ThrowIf();
                            }
                            break;
                    }
                };
            }
            catch (Exception)
            {
                image.Release(); // avoid memory leak if exception is thrown
                throw;
            }

            return new D3DScratchImage(image);
        }

        private static HResult HandleStbImageMemory(ImageFileFormat format, byte* data, int size, ref ScratchImage image)
        {
            if (StbImage.Is16BitFromMemory(data, size) == 1)
            {
                return HandleStbImage16Memory(format, data, size, ref image);
            }

            int width, height, channels;
            byte* imgData = StbImage.LoadFromMemory(data, size, &width, &height, &channels, 0);

            return LoadStbImage(ref image, width, height, channels, imgData);
        }

        private static HResult LoadStbImage(ref ScratchImage image, int width, int height, int channels, byte* imgData)
        {
            if (imgData == null)
            {
                return -1;
            }

            Format fmt = GetDXGIFormatFromChannelCount(channels);
            var hresult = image.Initialize2D((int)fmt, (nuint)width, (nuint)height, 1, 1, CPFlags.None);
            if (hresult.IsFailure)
            {
                StbImage.ImageFree(imgData);
                return hresult;
            }

            var img = image.GetImages()[0];

            if (channels == 2) // Grayscale + Alpha
            {
                for (int i = 0; i < width * height; i++)
                {
                    byte gray = imgData[i * 2 + 0];
                    byte alpha = imgData[i * 2 + 1];
                    img.Pixels[i * 4 + 0] = gray;  // R
                    img.Pixels[i * 4 + 1] = gray;  // G
                    img.Pixels[i * 4 + 2] = gray;  // B
                    img.Pixels[i * 4 + 3] = alpha; // A
                }
            }
            if (channels == 3)
            {
                for (int i = 0; i < width * height; i++)
                {
                    img.Pixels[i * 4 + 0] = imgData[i * 3 + 0]; // R
                    img.Pixels[i * 4 + 1] = imgData[i * 3 + 1]; // G
                    img.Pixels[i * 4 + 2] = imgData[i * 3 + 2]; // B
                    img.Pixels[i * 4 + 3] = byte.MaxValue;      // A (full opacity)
                }
            }
            else
            {
                Memcpy(imgData, img.Pixels, img.RowPitch * img.Height);
            }

            StbImage.ImageFree(imgData);
            return 0;
        }

        private static HResult HandleStbImage16Memory(ImageFileFormat format, byte* data, int size, ref ScratchImage image)
        {
            int width, height, channels;
            ushort* imgData = StbImage.Load16FromMemory(data, size, &width, &height, &channels, 0);

            return LoadStbImage16(ref image, width, height, channels, imgData);
        }

        private static HResult LoadStbImage16(ref ScratchImage image, int width, int height, int channels, ushort* imgData)
        {
            if (imgData == null)
            {
                return -1;
            }

            Format fmt = GetDXGIFormatFromChannelCount(channels);
            var hresult = image.Initialize2D((int)fmt, (nuint)width, (nuint)height, 1, 1, CPFlags.None);
            if (hresult.IsFailure)
            {
                StbImage.ImageFree(imgData);
                return hresult;
            }

            var img = image.GetImages()[0];
            var pixels = (ushort*)img.Pixels;

            if (channels == 2) // Grayscale + Alpha
            {
                for (int i = 0; i < width * height; i++)
                {
                    ushort gray = imgData[i * 2 + 0];
                    ushort alpha = imgData[i * 2 + 1];
                    pixels[i * 4 + 0] = gray;  // R
                    pixels[i * 4 + 1] = gray;  // G
                    pixels[i * 4 + 2] = gray;  // B
                    pixels[i * 4 + 3] = alpha; // A
                }
            }
            if (channels == 3)
            {
                for (int i = 0; i < width * height; i++)
                {
                    pixels[i * 4 + 0] = imgData[i * 3 + 0]; // R
                    pixels[i * 4 + 1] = imgData[i * 3 + 1]; // G
                    pixels[i * 4 + 2] = imgData[i * 3 + 2]; // B
                    pixels[i * 4 + 3] = ushort.MaxValue;    // A (full opacity)
                }
            }
            else
            {
                Memcpy(imgData, img.Pixels, img.RowPitch * img.Height);
            }

            StbImage.ImageFree(imgData);
            return 0;
        }

        private static Format GetDXGIFormatFromChannelCount(int channels)
        {
            return channels switch
            {
                1 => Format.R8Unorm,
                2 => Format.R8G8B8A8Unorm,
                3 => Format.R8G8B8A8Unorm,
                4 => Format.R8G8B8A8Unorm,
                _ => Format.Unknown
            };
        }

        private static Format GetDXGIFormatFromChannelCount16(int channels)
        {
            return channels switch
            {
                1 => Format.R16Unorm,
                2 => Format.R16G16B16A16Unorm,
                3 => Format.R16G16B16A16Unorm,
                4 => Format.R16G16B16A16Unorm,
                _ => Format.Unknown
            };
        }

        public static D3DScratchImage Initialize(TexMetadata metadata, CPFlags flags)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize(ref metadata, flags);
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage Initialize1D(Format fmt, int length, int arraySize, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize1D((int)fmt, (nuint)length, (nuint)arraySize, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage Initialize2D(Format fmt, int width, int height, int arraySize, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize2D((int)fmt, (nuint)width, (nuint)height, (nuint)arraySize, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage Initialize3D(Format fmt, int width, int height, int depth, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize3D((int)fmt, (nuint)width, (nuint)height, (nuint)depth, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage Initialize3DFromImages(Image[] images, int depth, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.Initialize3DFromImages(pImages, (nuint)depth, flags);
            }

            return new D3DScratchImage(image);
        }

        public static D3DScratchImage InitializeArrayFromImages(Image[] images, bool allow1D = false, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.InitializeArrayFromImages(pImages, (nuint)images.Length, allow1D, flags);
            }

            return new D3DScratchImage(image);
        }

        public static D3DScratchImage InitializeCube(Format fmt, int width, int height, int nCubes, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.InitializeCube((int)fmt, (nuint)width, (nuint)height, (nuint)nCubes, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public static D3DScratchImage InitializeCubeFromImages(Image[] images, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.InitializeCubeFromImages(pImages, (nuint)images.Length, flags);
            }

            return new D3DScratchImage(image);
        }

        public static D3DScratchImage InitializeFromImage(Image image, bool allow1D = false, CPFlags flags = CPFlags.None)
        {
            ScratchImage scratchImage = DirectXTex.CreateScratchImage();
            scratchImage.InitializeFromImage(image, allow1D, flags);
            return new D3DScratchImage(scratchImage);
        }
    }
}