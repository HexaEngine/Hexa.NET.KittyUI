namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.SDL2;
    using Hexa.NET.Utilities;
    using System.Collections.Concurrent;
    using System.Threading;

    public unsafe class UploadQueue
    {
        private readonly ConcurrentQueue<Pointer<OpenGLTextureTask>> creationQueue = new();
        private readonly ConcurrentQueue<Pointer<OpenGLTextureTask>> finishingQueue = new();
        private readonly List<Pointer<OpenGLTextureTask>> waitingList = new();
        private readonly ManualResetEventSlim signal = new(false);
        private readonly Thread uploadThread;

        private bool running = true;
        private ulong pollingRateMax = 1_000_000;
        private readonly HexaGen.Runtime.IGLContext context;

        private GL GL;

        public UploadQueue(HexaGen.Runtime.IGLContext mainContext, IWindow window)
        {
            mainContext.MakeCurrent();
            SDL.GLSetAttribute(SDLGLattr.GlShareWithCurrentContext, 1);
            context = window.OpenGLCreateContext();

            mainContext.MakeCurrent();

            uploadThread = new Thread(ThreadVoid)
            {
                Name = "GL Upload Thread"
            };
            uploadThread.Start();
        }

        public ulong PollingRateMax { get => pollingRateMax; set => pollingRateMax = value; }

        private void ThreadVoid()
        {
            context.MakeCurrent();
            GL = new(context);

            OpenGLPixelBufferPool.Global = new(GL);

            OpenGLTexturePool.Global = new(GL);
            OpenGLTexturePool.Global.AllocateNewBlock();

            while (running)
            {
                signal.Wait();

                if (!running)
                {
                    break;
                }

                ProcessQueue();

                signal.Reset();
            }

            context.Dispose();
        }

        /// <summary>
        /// Enqueues a task to be processed on the current thread.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns <c>true</c> if the task was enqueued, <c>false</c> if the current thread is the same as the thread that created the queue.</returns>
        public bool Enqueue(Pointer<OpenGLTextureTask> task)
        {
            creationQueue.Enqueue(task);
            signal.Set();
            return true;
        }

        public void EnqueueFinish(Pointer<OpenGLTextureTask> task)
        {
            finishingQueue.Enqueue(task);
            signal.Set();
        }

        public void ProcessQueue()
        {
            while (creationQueue.TryDequeue(out var task))
            {
                task.Data->CreateTexture(GL);
            }

            while (finishingQueue.TryDequeue(out var task))
            {
                task.Data->FinishTexture(GL);
                waitingList.Add(task);
            }

            while (waitingList.Count > 0)
            {
                int count = waitingList.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    ulong timeout = pollingRateMax / (ulong)count;
                    if (waitingList[i].Data->CheckIfDone(GL, timeout))
                    {
                        waitingList.RemoveAt(i);
                        count--;
                    }
                }
            }
        }

        public void Dispose()
        {
            running = false;
            signal.Set();
            uploadThread.Join();
            signal.Dispose();
        }
    }
}