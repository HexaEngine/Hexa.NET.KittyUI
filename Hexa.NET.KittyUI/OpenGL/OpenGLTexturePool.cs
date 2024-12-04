namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

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
        private readonly GL GL;

        public OpenGLTexturePool(GL GL)
        {
            mainThread = Thread.CurrentThread;
            this.GL = GL;
        }

        public OpenGLTexturePool(Thread thread)
        {
            mainThread = thread;
        }

        public static OpenGLTexturePool Global { get; internal set; } = null!;

        public uint GetNextTexture()
        {
            semaphore.Wait();

            int index = current;
            current++;

            if (index >= bufferBlockSize)
            {
                if (Thread.CurrentThread == mainThread)
                {
                    AllocateNewBlock(false);
                }

                semaphore.Release();

                while (Volatile.Read(ref current) >= bufferBlockSize)
                {
                    Thread.Yield();
                }

                semaphore.Wait();

                index = current;
                current++;
            }

            semaphore.Release();

            return textures[index];
        }

        public void AllocateNewBlock()
        {
            AllocateNewBlock(true);
        }

        private void AllocateNewBlock(bool lockBlock)
        {
            if (current < bufferBlockSize) return;
            if (Thread.CurrentThread != mainThread) return;

            if (lockBlock)
            {
                semaphore.Wait();
            }

            int toGenerate = bufferBlockSize - freeList.Size;
            GL.GenTextures(toGenerate, textures);

            MemcpyT(freeList.Data, textures + toGenerate, freeList.Size);
            freeList.Clear();

            Interlocked.Exchange(ref current, 0);

            if (lockBlock)
            {
                semaphore.Release();
            }
        }

        public void Return(uint texture)
        {
            semaphore.Wait();

            if (current == 0)
            {
                freeList.PushBack(texture); // this can never fail.
                return;
            }

            current--;
            textures[current] = texture;

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