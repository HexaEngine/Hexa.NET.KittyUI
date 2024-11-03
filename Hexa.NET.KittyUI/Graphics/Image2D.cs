namespace Hexa.NET.KittyUI.Graphics
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.Graphics.Imaging;
    using Hexa.NET.KittyUI.OpenGL;
    using Hexa.NET.OpenGL;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using ID3D11Device = NET.D3D11.ID3D11Device;
    using ID3D11Resource = NET.D3D11.ID3D11Resource;
    using ID3D11ShaderResourceView = NET.D3D11.ID3D11ShaderResourceView;

    /// <summary>
    /// The abstract base for all backends.
    /// </summary>
    public abstract unsafe class Image2D : DisposableBase
    {
        public static readonly TextureLoader TextureLoader = new();

        public abstract nint Handle { get; protected set; }

        public static implicit operator ImTextureID(Image2D image) => new(image.Handle);

        public int Width => (int)Metadata.Width;

        public int Height => (int)Metadata.Height;

        public abstract TexMetadata Metadata { get; }

        public static Image2D LoadFromFile(string path)
        {
            using var scratchImage = TextureLoader.LoadFormFile(path);
            return CreateImage(scratchImage);
        }

        public static Image2D LoadFromMemory(ImageFileFormat format, ReadOnlySpan<byte> data)
        {
            using var scratchImage = TextureLoader.LoadFromMemory(format, data);
            return CreateImage(scratchImage);
        }

        public static Image2D LoadFromMemory(ImageFileFormat format, byte[] data, int start, int length)
        {
            return LoadFromMemory(format, data.AsSpan(start, length));
        }

        public static Image2D CreateImage(D3DScratchImage scratchImage)
        {
            return Application.GraphicsBackend switch
            {
                GraphicsBackend.D3D11 => new D3D11Image(scratchImage),
                GraphicsBackend.OpenGL => new OpenGLImage(scratchImage),
                _ => throw new NotSupportedException(),
            };
        }
    }

    public unsafe class D3D11Image : Image2D
    {
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> srv;

        public D3D11Image(D3DScratchImage scratchImage)
        {
            var device = D3D11GraphicsDevice.Device;
            texture = scratchImage.CreateTexture2D((ID3D11Device*)device.Handle, Usage.Immutable, BindFlag.ShaderResource, 0, 0);
            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), null, out srv);
            Metadata = scratchImage.Metadata;
            Handle = (nint)srv.Handle;
        }

        public override nint Handle { get; protected set; }

        public override TexMetadata Metadata { get; }

        protected override void DisposeCore()
        {
            if (texture.Handle != null)
            {
                texture.Dispose();
                texture = default;
            }

            if (srv.Handle != null)
            {
                srv.Dispose();
                srv = default;
            }

            Handle = 0;
        }
    }

    public unsafe class OpenGLImage : Image2D
    {
        private uint texture;

        public OpenGLImage(D3DScratchImage scratchImage)
        {
            texture = scratchImage.CreateTexture2D();
            Metadata = scratchImage.Metadata;
            Handle = (nint)texture;
        }

        public override nint Handle { get; protected set; }

        public override TexMetadata Metadata { get; }

        protected override void DisposeCore()
        {
            if (texture != 0)
            {
                OpenGLAdapter.DeleteQueue.Enqueue(GLEnum.Texture, texture);
                Handle = 0;
                texture = 0;
            }
        }
    }
}