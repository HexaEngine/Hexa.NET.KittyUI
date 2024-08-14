namespace Hexa.NET.Kitty.Graphics.Imaging
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.Kitty.D3D11;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using System.IO;

    public enum TextureLoaderFlags
    {
        None = 0,
        GenerateMipMaps = 1,
        Scale = 2
    }

    public unsafe class TextureLoader
    {
        private TextureLoaderFlags flags = TextureLoaderFlags.GenerateMipMaps | TextureLoaderFlags.Scale;
        private float scalingFactor = 1;

        public TextureLoader()
        {
        }

        /// <summary>
        /// The Flags are only used for the LoadTextureXD functions which only load textures from assets.
        /// </summary>
        public TextureLoaderFlags Flags { get => flags; set => flags = value; }

        /// <summary>
        /// The ScalingFactor is only used for the LoadTextureXD functions which only load textures from assets.
        /// </summary>
        public float ScalingFactor { get => scalingFactor; set => scalingFactor = value; }

        public static D3DScratchImage CaptureTexture(ID3D11DeviceContext* context, ID3D11Resource* resource)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.CaptureTexture((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, context, resource, ref image).ThrowIf();
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
                        HandleWIC(filename, extension, ref image);
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

        private static void HandleWIC(string filename, ReadOnlySpan<char> extension, ref ScratchImage image)
        {
            if (OperatingSystem.IsWindows())
            {
                DirectXTex.LoadFromWICFile(filename, WICFlags.None, null, ref image, default).ThrowIf();
            }
            else
            {
                switch (extension)
                {
                    case ".png":
                        DirectXTex.LoadFromPNGFile(filename, null, ref image).ThrowIf();
                        break;

                    case ".jpg":
                        DirectXTex.LoadFromJPEGFile(filename, null, ref image).ThrowIf();
                        break;

                    default:
                        throw new NotSupportedException("Unsupported image format.");
                }
            }
        }

        public static D3DScratchImage LoadFromMemory(string filename, Stream stream)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            string extension = Path.GetExtension(filename);
            try
            {
                fixed (byte* p = data)
                {
                    switch (extension)
                    {
                        case ".dds":
                            DirectXTex.LoadFromDDSMemory(p, (nuint)data.Length, DDSFlags.None, null, ref image).ThrowIf();
                            break;

                        case ".tga":
                            DirectXTex.LoadFromTGAMemory(p, (nuint)data.Length, TGAFlags.None, null, ref image).ThrowIf();
                            break;

                        case ".hdr":
                            DirectXTex.LoadFromHDRMemory(p, (nuint)data.Length, null, ref image).ThrowIf();
                            break;

                        default:
                            if (OperatingSystem.IsWindows())
                            {
                                DirectXTex.LoadFromWICMemory(p, (nuint)data.Length, WICFlags.None, null, ref image, default).ThrowIf();
                            }
                            else
                            {
                                throw new PlatformNotSupportedException("Only Windows is supported for WIC. Use load from file overload for linux systems for PNG/JPEG.");
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