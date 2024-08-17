namespace Hexa.NET.KittyUI.OpenGL
{
    using Silk.NET.OpenGL;
    using System.Collections.Concurrent;

    public class DeleteQueue
    {
        private readonly ConcurrentQueue<(GLEnum, uint)> queue = new();
        private readonly GL gl;
        private readonly Thread thread;

        public DeleteQueue(GL gl, Thread thread)
        {
            this.gl = gl;
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
                    gl.DeleteTexture(item.Item2);
                    break;

                case GLEnum.Buffer:
                    gl.DeleteBuffer(item.Item2);
                    break;

                case GLEnum.Framebuffer:
                    gl.DeleteFramebuffer(item.Item2);
                    break;

                case GLEnum.Renderbuffer:
                    gl.DeleteRenderbuffer(item.Item2);
                    break;

                case GLEnum.VertexArray:
                    gl.DeleteVertexArray(item.Item2);
                    break;

                case GLEnum.ProgramPipeline:
                    gl.DeleteProgramPipeline(item.Item2);
                    break;

                case GLEnum.Program:
                    gl.DeleteProgram(item.Item2);
                    break;

                case GLEnum.Shader:
                    gl.DeleteShader(item.Item2);
                    break;

                case GLEnum.Sampler:
                    gl.DeleteSampler(item.Item2);
                    break;

                case GLEnum.TransformFeedback:
                    gl.DeleteTransformFeedback(item.Item2);
                    break;

                case GLEnum.Query:
                    gl.DeleteQuery(item.Item2);
                    break;
            }
        }
    }
}