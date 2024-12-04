namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;
    using Hexa.NET.OpenGLES.EXT;

#else

    using Hexa.NET.OpenGL;
    using Hexa.NET.OpenGL.ARB;

#endif

    using Hexa.NET.Utilities;
    using System;
    using System.Threading;

    public unsafe class OpenGLPixelBufferPool
    {
        private UnsafeList<PixelUnpackBufferPoolObject> objects = [];
        private UnsafeList<PixelUnpackBufferPoolObject> free = [];
        private readonly SemaphoreSlim semaphore = new(1);
        private readonly AutoResetEvent waitHandle = new(false);
        private readonly GL GL;
        private const nint DefaultBufferSize = 16_777_216; // 16 MiB
        private const int DefaultBufferCount = 16;

#if GLES
        private readonly GLEXTBufferStorage? GLEXTBufferStorage;
        private readonly GLEXTMapBufferRange? GLEXTMapBufferRange;
#else
        private readonly GLARBBufferStorage? GLARBBufferStorage;
        private readonly GLARBMapBufferRange? GLARBMapBufferRange;
#endif

        public OpenGLPixelBufferPool(GL GL)
        {
            this.GL = GL;
            uint* buffers = stackalloc uint[DefaultBufferCount];
            GL.GenBuffers(DefaultBufferCount, buffers);
            objects.Reserve(DefaultBufferCount);
            free.Reserve(DefaultBufferCount);
            for (int i = 0; i < DefaultBufferCount; i++)
            {
                PixelUnpackBufferPoolObject poolObject = MakeBuffer(DefaultBufferSize, buffers[i]);
                objects.Add(poolObject);
                free.Add(poolObject);
            }
            if (!OpenGLAdapter.NoExtensions)
            {
#if GLES

                if (!GL.TryGetExtension(out GLEXTBufferStorage))
                {
                    GLEXTBufferStorage = null;
                }
                if (!GL.TryGetExtension(out GLEXTMapBufferRange))
                {
                    GLEXTMapBufferRange = null;
                }

#else

                if (!GL.TryGetExtension(out GLARBBufferStorage))
                {
                    GLARBBufferStorage = null;
                }
                if (!GL.TryGetExtension(out GLARBMapBufferRange))
                {
                    GLARBMapBufferRange = null;
                }

#endif
            }
        }

        private PixelUnpackBufferPoolObject MakeBuffer(nint bufferSize, uint buffer)
        {
            PixelUnpackBufferPoolObject poolObject = new()
            {
                Buffer = buffer,
                Size = bufferSize,
            };

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, buffer);

            if (OpenGLAdapter.IsPersistentMappingSupported)
            {
                GLMapBufferAccessMask mask = GLMapBufferAccessMask.WriteBit | GLMapBufferAccessMask.PersistentBit | GLMapBufferAccessMask.UnsynchronizedBit | GLMapBufferAccessMask.FlushExplicitBit;

                GL.BufferStorage(GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null, GLBufferStorageMask.MapPersistentBit | GLBufferStorageMask.MapWriteBit);

                poolObject.MappedData = GL.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
            }
#if GLES
            else if (!OpenGLAdapter.NoExtensions && GLEXTBufferStorage != null && GLEXTMapBufferRange != null)
            {
                GLMapBufferAccessMask mask = GLMapBufferAccessMask.WriteBitExt | GLMapBufferAccessMask.PersistentBitExt | GLMapBufferAccessMask.UnsynchronizedBitExt | GLMapBufferAccessMask.FlushExplicitBitExt;

                GLEXTBufferStorage.BufferStorageEXT(GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null, GLBufferStorageMask.MapPersistentBit | GLBufferStorageMask.MapWriteBit);

                poolObject.MappedData = GLEXTMapBufferRange.MapBufferRangeEXT(GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
            }
#else
            else if (GLARBBufferStorage != null && GLARBMapBufferRange != null)
            {
                GLMapBufferAccessMask mask = GLMapBufferAccessMask.WriteBit | GLMapBufferAccessMask.PersistentBit | GLMapBufferAccessMask.UnsynchronizedBit | GLMapBufferAccessMask.FlushExplicitBit;
                GLARBBufferStorage.BufferStorage(GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null,
                    GLBufferStorageMask.MapPersistentBit |
                    GLBufferStorageMask.MapWriteBit);
                poolObject.MappedData = GLARBMapBufferRange.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
            }
#endif
            else
            {
                GL.BufferData(GLBufferTargetARB.PixelUnpackBuffer, bufferSize, null, GLBufferUsageARB.StreamDraw);
            }

            GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);

            return poolObject;
        }

        public static OpenGLPixelBufferPool Global { get; internal set; } = null!;

        public PixelUnpackBufferPoolObject Rent(nint size)
        {
            PixelUnpackBufferPoolObject poolObject;
            RentRange(&poolObject, 1, &size);
            return poolObject;
        }

        public void RentRange(PixelUnpackBufferPoolObject* objs, int count, nint* sizes)
        {
            if (objects.Count < count)
            {
                semaphore.Wait();

                if (objects.Count < count) // check another time, a diffrent thread could had already done the job.
                {
                    AddNewBuffers(count);
                }

                semaphore.Release();
            }

            int bufIdx = 0;
            while (true)
            {
                semaphore.Wait();

                for (int i = free.Size - 1; i >= 0; i--)
                {
                    var freeObj = free[i];
                    if (freeObj.Size >= sizes[bufIdx])
                    {
                        free.RemoveAt(i);
                        objs[bufIdx] = freeObj;
                        bufIdx++;

                        if (bufIdx >= count)
                        {
                            semaphore.Release();
                            return;
                        }
                    }
                }

                if (free.Size > 0)
                {
                    int start = Math.Max(free.Size - (count - bufIdx), 0);

                    for (int i = free.Size - 1; bufIdx < count && i >= start; bufIdx++, i--)
                    {
                        objs[bufIdx] = MakeBuffer(sizes[bufIdx], free[i].Buffer);
                        free.RemoveAt(i);
                    }

                    semaphore.Release();

                    if (bufIdx >= count)
                    {
                        return;
                    }
                }
                else
                {
                    semaphore.Release();

                    waitHandle.WaitOne();
                }
            }
        }

        private void AddNewBuffers(int min)
        {
            int newSize = (int)MathF.Ceiling(min / (float)DefaultBufferCount);
            int newCount = newSize - objects.Count;

            uint* buffers = stackalloc uint[newCount];
            GL.GenBuffers(newCount, buffers);
            objects.Reserve(newSize);
            free.Reserve(newSize);
            for (int i = 0; i < newCount; i++)
            {
                PixelUnpackBufferPoolObject poolObject = MakeBuffer(DefaultBufferSize, buffers[i]);
                objects.Add(poolObject);
                free.Add(poolObject);
            }
        }

        public void Return(PixelUnpackBufferPoolObject obj)
        {
            semaphore.Wait();

            free.PushBack(obj);

            waitHandle.Set();

            semaphore.Release();
        }

        public void Dispose()
        {
            semaphore.Wait();
            foreach (var obj in objects)
            {
                GL.DeleteBuffer(obj.Buffer);
            }
            semaphore.Release();
            semaphore.Dispose();
            waitHandle.Dispose();
        }
    }
}