namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;
    using Hexa.NET.OpenGL.ARB;
    using Hexa.NET.OpenGLES.EXT;
    using Hexa.NET.Utilities;
    using System;
    using System.Threading;
    using GLES = OpenGLES.GL;

    public unsafe class OpenGLPixelBufferPool
    {
        private UnsafeList<PixelUnpackBufferPoolObject> objects = [];
        private UnsafeList<PixelUnpackBufferPoolObject> free = [];
        private readonly SemaphoreSlim semaphore = new(1);
        private readonly AutoResetEvent waitHandle = new(false);
        private const nint DefaultBufferSize = 16_777_216; // 16 MiB
        private const int DefaultBufferCount = 16;

        public OpenGLPixelBufferPool()
        {
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
        }

        private static PixelUnpackBufferPoolObject MakeBuffer(nint bufferSize, uint buffer)
        {
            PixelUnpackBufferPoolObject poolObject = new()
            {
                Buffer = buffer,
                Size = bufferSize,
            };

            if (GLVersion.Current.ES)
            {
                GLES.BindBuffer(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, buffer);

                if (OpenGLAdapter.IsPersistentMappingSupported)
                {
                    OpenGLES.GLMapBufferAccessMask mask = OpenGLES.GLMapBufferAccessMask.WriteBit | OpenGLES.GLMapBufferAccessMask.PersistentBit | OpenGLES.GLMapBufferAccessMask.UnsynchronizedBit | OpenGLES.GLMapBufferAccessMask.FlushExplicitBit;

                    GLES.BufferStorage(OpenGLES.GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null, OpenGLES.GLBufferStorageMask.MapPersistentBit | OpenGLES.GLBufferStorageMask.MapWriteBit);

                    poolObject.MappedData = GLES.MapBufferRange(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
                }
                else if (!OpenGLAdapter.NoExtensions && GLEXTBufferStorage.TryInitExtension() && GLEXTMapBufferRange.TryInitExtension())
                {
                    OpenGLES.GLMapBufferAccessMask mask = OpenGLES.GLMapBufferAccessMask.WriteBitExt | OpenGLES.GLMapBufferAccessMask.PersistentBitExt | OpenGLES.GLMapBufferAccessMask.UnsynchronizedBitExt | OpenGLES.GLMapBufferAccessMask.FlushExplicitBitExt;

                    GLEXTBufferStorage.BufferStorageEXT(OpenGLES.GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null, OpenGLES.GLBufferStorageMask.MapPersistentBit | OpenGLES.GLBufferStorageMask.MapWriteBit);

                    poolObject.MappedData = GLEXTMapBufferRange.MapBufferRangeEXT(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
                }
                else
                {
                    GLES.BufferData(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, bufferSize, null, OpenGLES.GLBufferUsageARB.StreamDraw);
                }

                GLES.BindBuffer(OpenGLES.GLBufferTargetARB.PixelUnpackBuffer, 0);
            }
            else
            {
                GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, buffer);

                if (OpenGLAdapter.IsPersistentMappingSupported)
                {
                    GLMapBufferAccessMask mask = GLMapBufferAccessMask.WriteBit | GLMapBufferAccessMask.PersistentBit | GLMapBufferAccessMask.UnsynchronizedBit | GLMapBufferAccessMask.FlushExplicitBit;

                    GL.BufferStorage(GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null, GLBufferStorageMask.MapPersistentBit | GLBufferStorageMask.MapWriteBit);

                    poolObject.MappedData = GL.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
                }
                else if (!OpenGLAdapter.NoExtensions && GLARBBufferStorage.TryInitExtension() && GLARBMapBufferRange.TryInitExtension())
                {
                    GLMapBufferAccessMask mask = GLMapBufferAccessMask.WriteBit | GLMapBufferAccessMask.PersistentBit | GLMapBufferAccessMask.UnsynchronizedBit | GLMapBufferAccessMask.FlushExplicitBit;
                    GLARBBufferStorage.BufferStorage(GLBufferStorageTarget.PixelUnpackBuffer, bufferSize, null,
                        GLBufferStorageMask.MapPersistentBit |
                        GLBufferStorageMask.MapWriteBit);
                    poolObject.MappedData = GLARBMapBufferRange.MapBufferRange(GLBufferTargetARB.PixelUnpackBuffer, 0, bufferSize, mask);
                }
                else
                {
                    GL.BufferData(GLBufferTargetARB.PixelUnpackBuffer, bufferSize, null, GLBufferUsageARB.StreamDraw);
                }

                GL.BindBuffer(GLBufferTargetARB.PixelUnpackBuffer, 0);
            }

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