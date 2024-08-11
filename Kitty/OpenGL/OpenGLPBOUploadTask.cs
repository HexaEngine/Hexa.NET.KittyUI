namespace Hexa.NET.Kitty.OpenGL
{
    using Silk.NET.OpenGL;
    using System.Threading;
    using System.Threading.Tasks;

    public unsafe struct OpenGLPBOUploadTask
    {
        private readonly nuint size;
        private uint _pboId;
        private int mapped;
        private void* pData;

        public OpenGLPBOUploadTask(nuint size)
        {
            mapped = 0;
            this.size = size;
        }

        public void MapBuffer(GL gl)
        {
            _pboId = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _pboId);
            gl.BufferData(BufferTargetARB.PixelUnpackBuffer, size, null, BufferUsageARB.StreamDraw);
            pData = gl.MapBuffer(BufferTargetARB.PixelUnpackBuffer, GLEnum.WriteOnly);
            mapped = 1;
        }

        public void UnmapBuffer(GL gl)
        {
            if (mapped == 1)
            {
                gl.UnmapNamedBuffer(_pboId);
                mapped = 0;
            }
        }

        public void WaitMapped()
        {
            while (Interlocked.CompareExchange(ref mapped, 1, 0) != 0)
            {
                Task.Yield();
            }
        }
    }
}