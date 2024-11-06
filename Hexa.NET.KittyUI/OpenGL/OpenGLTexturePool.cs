namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;
    using Hexa.NET.Utilities;
    using System.Threading;

    public unsafe class OpenGLTexturePool
    {
        private const int bufferBlockSize = 128;
        private int current = bufferBlockSize;
        private uint* textures = AllocT<uint>(bufferBlockSize);
        private readonly Thread mainThread;
        private UnsafeList<uint> freeList = [];
        private readonly SemaphoreSlim semaphore = new(1);

        public OpenGLTexturePool()
        {
            mainThread = Thread.CurrentThread;
        }

        public OpenGLTexturePool(Thread thread)
        {
            mainThread = thread;
        }

        public static OpenGLTexturePool Global { get; } = new();

        public uint GetNextTexture()
        {
            int index = Interlocked.Increment(ref current);

            if (index >= bufferBlockSize)
            {
                if (Thread.CurrentThread == mainThread)
                {
                    AllocateNewBlock();
                }
                while (Volatile.Read(ref current) >= bufferBlockSize)
                {
                    Thread.Yield();
                }
                index = Interlocked.Increment(ref current);
            }

            return textures[index];
        }

        public void AllocateNewBlock()
        {
            if (current < bufferBlockSize) return;
            if (Thread.CurrentThread != mainThread) return;

            semaphore.Wait();

            int toGenerate = bufferBlockSize - freeList.Size;
            GL.GenTextures(toGenerate, textures);

            MemcpyT(freeList.Data, textures + toGenerate, freeList.Size);
            freeList.Clear();

            Interlocked.Exchange(ref current, 0);

            semaphore.Release();
        }

        public void Return(uint texture)
        {
            semaphore.Wait();
            freeList.PushBack(texture); // this can never fail.
            semaphore.Release();
        }

        public void Dispose()
        {
            semaphore.Dispose();

            if (freeList.Data != null)
            {
                GL.DeleteTextures(freeList.Size, freeList.Data);
                freeList.Release();
            }

            if (textures != null)
            {
                int toDelete = bufferBlockSize - current;
                GL.DeleteTextures(toDelete, textures + current);
                Free(textures);
                textures = null;
            }
        }
    }
}