namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;
    using System.Collections.Concurrent;

    public class DeleteQueue
    {
        private readonly ConcurrentQueue<(GLEnum, uint)> queue = new();
        private readonly Thread thread;

        public DeleteQueue(Thread thread)
        {
            this.thread = thread;
        }

        public void Enqueue(GLEnum target, uint id)
        {
            if (Thread.CurrentThread == thread)
            {
                ProcessItem((target, id));
                return;
            }
            queue.Enqueue((target, id));
        }

        public void ProcessQueue()
        {
            while (queue.TryDequeue(out var item))
            {
                ProcessItem(item);
            }
        }

        private void ProcessItem((GLEnum, uint) item)
        {
            switch (item.Item1)
            {
                case GLEnum.Texture:
                    GL.DeleteTexture(item.Item2);
                    break;

                case GLEnum.Buffer:
                    GL.DeleteBuffer(item.Item2);
                    break;

                case GLEnum.Framebuffer:
                    GL.DeleteFramebuffer(item.Item2);
                    break;

                case GLEnum.Renderbuffer:
                    GL.DeleteRenderbuffer(item.Item2);
                    break;

                case GLEnum.VertexArray:
                    GL.DeleteVertexArray(item.Item2);
                    break;

                case GLEnum.ProgramPipeline:
                    GL.DeleteProgramPipeline(item.Item2);
                    break;

                case GLEnum.Program:
                    GL.DeleteProgram(item.Item2);
                    break;

                case GLEnum.Shader:
                    GL.DeleteShader(item.Item2);
                    break;

                case GLEnum.Sampler:
                    GL.DeleteSampler(item.Item2);
                    break;

                case GLEnum.TransformFeedback:
                    GL.DeleteTransformFeedback(item.Item2);
                    break;

                case GLEnum.Query:
                    GL.DeleteQuerie(item.Item2);
                    break;
            }
        }
    }
}