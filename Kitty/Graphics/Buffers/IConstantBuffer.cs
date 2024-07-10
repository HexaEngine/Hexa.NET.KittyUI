namespace Kitty.Graphics.Buffers
{
    using Kitty.Graphics;

    public interface IConstantBuffer : IResource
    {
        IBuffer Buffer { get; }

        void Resize(uint length);

        void Update(IGraphicsContext context);
    }

    public interface IConstantBuffer<T> : IConstantBuffer where T : unmanaged
    {
        T this[int index] { get; set; }

        unsafe T* Local { get; }
    }
}