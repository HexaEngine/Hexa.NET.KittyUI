namespace Hexa.NET.KittyUI.Graphics
{
    using Hexa.NET.DirectXTex;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI;
    using Hexa.NET.KittyUI.Graphics.Imaging;

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

        public static Image2D CreateImage(Imaging.ImageSource scratchImage)
        {
            return Application.GraphicsBackend switch
            {
                GraphicsBackend.D3D11 => new D3D11Image(scratchImage),
                GraphicsBackend.OpenGL => new OpenGLImage(scratchImage),
                _ => throw new NotSupportedException(),
            };
        }
    }
}