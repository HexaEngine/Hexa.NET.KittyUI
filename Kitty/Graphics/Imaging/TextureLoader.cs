namespace Hexa.NET.Kitty.Graphics.Imaging
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.Kitty.D3D11;
    using Hexa.NET.Mathematics;
    using Kitty.IO;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using System.Diagnostics;
    using System.IO;
    using System.Numerics;

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

        public D3DScratchImage CaptureTexture(ID3D11DeviceContext* context, ID3D11Resource* resource)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            DirectXTex.CaptureTexture((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, context, resource, ref image);

            return new D3DScratchImage(image);
        }

        public D3DScratchImage LoadFormAssets(string path)
        {
            if (!File.Exists(path))
            {
                Trace.WriteLine($"Warning couldn't find texture {path}");
                return InitFallback(ResourceDimension.Texture2D);
            }
            var fs = File.OpenRead(path);

            ScratchImage image = DirectXTex.CreateScratchImage();
            var data = fs.ReadBytes((int)fs.Length);
            string extension = Path.GetExtension(path);
            fixed (byte* p = data)
                switch (extension)
                {
                    case ".dds":
                        DirectXTex.LoadFromDDSMemory(p, (nuint)data.Length, DDSFlags.None, null, ref image);
                        break;

                    case ".tga":
                        DirectXTex.LoadFromTGAMemory(p, (nuint)data.Length, TGAFlags.None, null, ref image);
                        break;

                    case ".hdr":
                        DirectXTex.LoadFromHDRMemory(p, (nuint)data.Length, null, ref image);
                        break;

                    default:
                        DirectXTex.LoadFromWICMemory(p, (nuint)data.Length, WICFlags.None, null, ref image, default);
                        break;
                };
            return new D3DScratchImage(image);
        }

        public D3DScratchImage LoadFormAssets(string path, ResourceDimension dimension)
        {
            if (!File.Exists(path))
            {
                Trace.WriteLine($"Warning couldn't find texture {path}");
                return InitFallback(dimension);
            }
            var fs = File.OpenRead(path);

            ScratchImage image = DirectXTex.CreateScratchImage();
            var data = fs.ReadBytes((int)fs.Length);
            string extension = Path.GetExtension(path);
            fixed (byte* p = data)
                switch (extension)
                {
                    case ".dds":
                        DirectXTex.LoadFromDDSMemory(p, (nuint)data.Length, DDSFlags.None, null, ref image);
                        break;

                    case ".tga":
                        DirectXTex.LoadFromTGAMemory(p, (nuint)data.Length, TGAFlags.None, null, ref image);
                        break;

                    case ".hdr":
                        DirectXTex.LoadFromHDRMemory(p, (nuint)data.Length, null, ref image);
                        break;

                    default:
                        DirectXTex.LoadFromWICMemory(p, (nuint)data.Length, WICFlags.None, null, ref image, default);
                        break;
                };
            return new D3DScratchImage(image);
        }

        private D3DScratchImage InitFallback(ResourceDimension dimension)
        {
            Vector4 fallbackColor = new(1, 0, 1, 1);
            ScratchImage fallback = DirectXTex.CreateScratchImage();
            if (dimension == ResourceDimension.Texture1D)
            {
                fallback.Initialize1D((int)Format.FormatR32G32B32A32Float, 1, 1, 1, CPFlags.None);
            }
            if (dimension == ResourceDimension.Texture2D)
            {
                fallback.Initialize2D((int)Format.FormatR32G32B32A32Float, 1, 1, 1, 1, CPFlags.None);
            }
            if (dimension == ResourceDimension.Texture3D)
            {
                fallback.Initialize3D((int)Format.FormatR32G32B32A32Float, 1, 1, 1, 1, CPFlags.None);
            }

            var size = fallback.GetPixelsSize();
            for (ulong i = 0; i < 1; i++)
            {
                ((Vector4*)fallback.GetPixels())[i] = fallbackColor;
            }

            return new D3DScratchImage(fallback);
        }

        public D3DScratchImage LoadFormFile(string filename)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            string extension = Path.GetExtension(filename);
            switch (extension)
            {
                case ".dds":
                    DirectXTex.LoadFromDDSFile(filename, DDSFlags.None, null, ref image);
                    break;

                case ".tga":
                    DirectXTex.LoadFromTGAFile(filename, TGAFlags.None, null, ref image);
                    break;

                case ".hdr":
                    DirectXTex.LoadFromHDRFile(filename, null, ref image);
                    break;

                default:
                    DirectXTex.LoadFromWICFile(filename, WICFlags.None, null, ref image, default);
                    break;
            };

            return new D3DScratchImage(image);
        }

        public D3DScratchImage LoadFromMemory(string filename, Stream stream)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            string extension = Path.GetExtension(filename);
            fixed (byte* p = data)
                switch (extension)
                {
                    case ".dds":
                        DirectXTex.LoadFromDDSMemory(p, (nuint)data.Length, DDSFlags.None, null, ref image);
                        break;

                    case ".tga":
                        DirectXTex.LoadFromTGAMemory(p, (nuint)data.Length, TGAFlags.None, null, ref image);
                        break;

                    case ".hdr":
                        DirectXTex.LoadFromHDRMemory(p, (nuint)data.Length, null, ref image);
                        break;

                    default:
                        DirectXTex.LoadFromWICMemory(p, (nuint)data.Length, WICFlags.None, null, ref image, default);
                        break;
                };

            return new D3DScratchImage(image);
        }

        public D3DScratchImage Initialize(TexMetadata metadata, CPFlags flags)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize(ref metadata, flags);
            return new D3DScratchImage(image);
        }

        public D3DScratchImage Initialize1D(Format fmt, int length, int arraySize, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize1D((int)fmt, (nuint)length, (nuint)arraySize, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public D3DScratchImage Initialize2D(Format fmt, int width, int height, int arraySize, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize2D((int)fmt, (nuint)width, (nuint)height, (nuint)arraySize, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public D3DScratchImage Initialize3D(Format fmt, int width, int height, int depth, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.Initialize3D((int)fmt, (nuint)width, (nuint)height, (nuint)depth, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public D3DScratchImage Initialize3DFromImages(Image[] images, int depth, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.Initialize3DFromImages(pImages, (nuint)depth, flags);
            }

            return new D3DScratchImage(image);
        }

        public D3DScratchImage InitializeArrayFromImages(Image[] images, bool allow1D = false, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.InitializeArrayFromImages(pImages, (nuint)images.Length, allow1D, flags);
            }

            return new D3DScratchImage(image);
        }

        public D3DScratchImage InitializeCube(Format fmt, int width, int height, int nCubes, int mipLevels, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            image.InitializeCube((int)fmt, (nuint)width, (nuint)height, (nuint)nCubes, (nuint)mipLevels, flags);
            return new D3DScratchImage(image);
        }

        public D3DScratchImage InitializeCubeFromImages(Image[] images, CPFlags flags = CPFlags.None)
        {
            ScratchImage image = DirectXTex.CreateScratchImage();
            fixed (Image* pImages = images)
            {
                image.InitializeCubeFromImages(pImages, (nuint)images.Length, flags);
            }

            return new D3DScratchImage(image);
        }

        public D3DScratchImage InitializeFromImage(Image image, bool allow1D = false, CPFlags flags = CPFlags.None)
        {
            ScratchImage scratchImage = DirectXTex.CreateScratchImage();
            scratchImage.InitializeFromImage(image, allow1D, flags);
            return new D3DScratchImage(scratchImage);
        }

        public ComPtr<ID3D11Texture1D> LoadTexture1D(string path, Usage usage, BindFlag bind, CpuAccessFlag cpuAccess, ResourceMiscFlag misc)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture1D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            var tex = image.CreateTexture1D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, usage, bind, cpuAccess, misc);

            image.Dispose();
            return tex;
        }

        public ComPtr<ID3D11Texture2D> LoadTexture2D(string path, Usage usage, BindFlag bind, CpuAccessFlag cpuAccess, ResourceMiscFlag misc)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture2D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            var tex = image.CreateTexture2D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, usage, bind, cpuAccess, misc);

            image.Dispose();
            return tex;
        }

        public ComPtr<ID3D11Texture3D> LoadTexture3D(string path, Usage usage, BindFlag bind, CpuAccessFlag cpuAccess, ResourceMiscFlag misc)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture3D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            var tex = image.CreateTexture3D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, usage, bind, cpuAccess, misc);

            image.Dispose();
            return tex;
        }

        public ComPtr<ID3D11Texture1D> LoadTexture1D(string path)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture1D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            var tex = image.CreateTexture1D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, Usage.Immutable, BindFlag.ShaderResource, CpuAccessFlag.None, ResourceMiscFlag.None);

            image.Dispose();
            return tex;
        }

        public ComPtr<ID3D11Texture2D> LoadTexture2D(string path)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture2D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            ResourceMiscFlag miscFlag = 0;
            if (image.Metadata.IsCubemap())
            {
                miscFlag = ResourceMiscFlag.Texturecube;
            }

            var tex = image.CreateTexture2D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, Usage.Immutable, BindFlag.ShaderResource, CpuAccessFlag.None, miscFlag);

            image.Dispose();
            return tex;
        }

        public ComPtr<ID3D11Texture3D> LoadTexture3D(string path)
        {
            var image = LoadFormAssets(path, ResourceDimension.Texture3D);
            if ((flags & TextureLoaderFlags.Scale) != 0 && scalingFactor != 1)
            {
                var tmp = image.Resize(scalingFactor, TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }
            if ((flags & TextureLoaderFlags.GenerateMipMaps) != 0 && image.Metadata.MipLevels == 1 && image.Metadata.Width > 1 && image.Metadata.Height > 1)
            {
                var tmp = image.GenerateMipMaps(TexFilterFlags.Default);
                image.Dispose();
                image = tmp;
            }

            var tex = image.CreateTexture3D((ID3D11Device*)D3D11GraphicsDevice.Device.Handle, Usage.Immutable, BindFlag.ShaderResource, CpuAccessFlag.None, ResourceMiscFlag.None);

            image.Dispose();
            return tex;
        }
    }
}