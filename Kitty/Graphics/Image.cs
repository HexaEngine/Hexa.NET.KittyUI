namespace Hexa.NET.Kitty.Graphics
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Kitty;

    /// <summary>
    /// The abstract base for all backends.
    /// </summary>
    public abstract class Image : DisposableBase
    {
        public abstract nint Handle { get; }

        public static implicit operator ImTextureID(Image image) => new(image.Handle);

        public static void LoadFromFile(string path)
        {
            switch (Application.GraphicsBackend)
            {
            }
        }
    }
}