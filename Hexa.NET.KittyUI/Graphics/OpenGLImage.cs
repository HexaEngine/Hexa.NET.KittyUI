namespace Hexa.NET.KittyUI.Graphics
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.KittyUI.OpenGL;

    public unsafe class OpenGLImage : Image2D
    {
        private uint texture;

        public OpenGLImage(Imaging.ImageSource scratchImage)
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
                OpenGLTexturePool.Global.Return(texture);
                Handle = 0;
                texture = 0;
            }
        }
    }
}