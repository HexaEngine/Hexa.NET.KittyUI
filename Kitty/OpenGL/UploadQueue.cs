namespace Hexa.NET.Kitty.OpenGL
{
    using Hexa.NET.Utilities;
    using Silk.NET.OpenGL;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public unsafe class UploadQueue
    {
        private readonly ConcurrentQueue<Pointer<OpenGLTextureTask>> creationQueue = new();
        private readonly ConcurrentQueue<Pointer<OpenGLTextureTask>> finishingQueue = new();
        private readonly List<Pointer<OpenGLTextureTask>> waitingList = new();
        private readonly GL gl;
        private readonly Thread currentThread;

        public UploadQueue(GL gl, Thread currentThread)
        {
            this.gl = gl;
            this.currentThread = currentThread;
        }

        /// <summary>
        /// Enqueues a task to be processed on the current thread.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns <c>true</c> if the task was enqueued, <c>false</c> if the current thread is the same as the thread that created the queue.</returns>
        public bool Enqueue(Pointer<OpenGLTextureTask> task)
        {
            if (Thread.CurrentThread == currentThread)
            {
                return false;
            }
            creationQueue.Enqueue(task);
            return true;
        }

        public void EnqueueFinish(Pointer<OpenGLTextureTask> task)
        {
            finishingQueue.Enqueue(task);
        }

        public void ProcessQueue()
        {
            while (creationQueue.TryDequeue(out var task))
            {
                task.Data->CreateTexture(gl);
            }

            while (finishingQueue.TryDequeue(out var task))
            {
                task.Data->FinishTexture(gl);
                waitingList.Add(task);
            }

            for (int i = waitingList.Count - 1; i >= 0; i--)
            {
                if (waitingList[i].Data->CheckIfDone(gl))
                {
                    waitingList.RemoveAt(i);
                }
            }
        }
    }
}