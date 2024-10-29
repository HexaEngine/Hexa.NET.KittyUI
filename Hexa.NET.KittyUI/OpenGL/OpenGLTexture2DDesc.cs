namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;

    public struct OpenGLTexture2DDesc
    {
        public int Width;
        public int Height;
        public uint MipLevels;
        public uint ArraySize;
        public GLInternalFormat InternalFormat;
        public GLPixelFormat PixelFormat;
        public GLPixelType PixelType;
        public GLTextureWrapMode WrapS;
        public GLTextureWrapMode WrapT;
        public GLTextureMinFilter MinFilter;
        public GLTextureMagFilter MagFilter;
    }
}