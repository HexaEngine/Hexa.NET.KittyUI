namespace Kitty.Graphics
{
    public unsafe interface IBuffer : IResource
    {
        public BufferDescription Description { get; }

        public int Length { get; }
    }
}