namespace Hexa.NET.Kitty.OpenGL
{
    using Silk.NET.OpenGL;

    public struct OpenGLTexture2DDesc
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public InternalFormat InternalFormat;
        public PixelFormat PixelFormat;
        public PixelType PixelType;
        public TextureWrapMode WrapS;
        public TextureWrapMode WrapT;
        public TextureMinFilter MinFilter;
        public TextureMagFilter MagFilter;
    }
}