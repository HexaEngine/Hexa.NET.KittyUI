namespace Hexa.NET.Kitty.Graphics
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Kitty;
    using Hexa.NET.Kitty.D3D11;
    using Hexa.NET.Kitty.Graphics.Imaging;
    using Hexa.NET.Kitty.OpenGL;
    using Hexa.NET.Kitty.Windows;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.OpenGL;

    /// <summary>
    /// The abstract base for all backends.
    /// </summary>
    public abstract unsafe class Image : DisposableBase
    {
        public static readonly TextureLoader TextureLoader = new();

        public abstract nint Handle { get; protected set; }

        public static implicit operator ImTextureID(Image image) => new(image.Handle);

        public static Image LoadFromFile(string path)
        {
            var scratchImage = TextureLoader.LoadFormFile(path);

            switch (Application.GraphicsBackend)
            {
                case GraphicsBackend.D3D11:
                    var device = D3D11GraphicsDevice.Device;
                    var tex = scratchImage.CreateTexture2D((ID3D11Device*)device.Handle, Usage.Immutable, BindFlag.ShaderResource, CpuAccessFlag.None, ResourceMiscFlag.None);
                    ComPtr<ID3D11ShaderResourceView> srv = default;
                    device.CreateShaderResourceView(tex, null, ref srv);
                    return new D3D11Image(tex, srv);

                case GraphicsBackend.OpenGL:
                    var gl = OpenGLAdapter.GL;
                    var texId = scratchImage.CreateTexture2D(gl);
                    return new OpenGLImage(texId);

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public unsafe class D3D11Image : Image
    {
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> srv;

        public D3D11Image(ComPtr<ID3D11Texture2D> texture, ComPtr<ID3D11ShaderResourceView> srv)
        {
            this.texture = texture;
            this.srv = srv;
            Handle = (nint)srv.Handle;
        }

        public override nint Handle { get; protected set; }

        protected override void DisposeCore()
        {
            texture.Dispose();
            srv.Dispose();
            Handle = 0;
        }
    }

    public unsafe class OpenGLImage : Image
    {
        private uint texture;

        public OpenGLImage(uint texture)
        {
            this.texture = texture;
            Handle = (nint)texture;
        }

        public override nint Handle { get; protected set; }

        protected override void DisposeCore()
        {
            OpenGLAdapter.DeleteQueue.Enqueue(GLEnum.Texture, texture);
            Handle = 0;
        }
    }
}