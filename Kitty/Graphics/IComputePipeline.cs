namespace Kitty.Graphics
{
    public interface IComputePipeline : IDisposable
    {
        void BeginDispatch(IGraphicsContext context);

        void Dispatch(IGraphicsContext context, int x, int y, int z);

        void EndDispatch(IGraphicsContext context);

        void Recompile();
    }
}