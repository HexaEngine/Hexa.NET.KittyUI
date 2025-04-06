namespace Hexa.NET.KittyUI.Graphics
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DirectXTex;
    using Hexa.NET.KittyUI.D3D11;
    using HexaGen.Runtime.COM;
    using ID3D11Device = NET.D3D11.ID3D11Device;
    using ID3D11Resource = NET.D3D11.ID3D11Resource;
    using ID3D11ShaderResourceView = NET.D3D11.ID3D11ShaderResourceView;

    public unsafe class D3D11Image : Image2D
    {
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> srv;

        public D3D11Image(Imaging.ImageSource scratchImage)
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
}