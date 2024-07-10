namespace Kitty.Graphics
{
    using Silk.NET.Core.Contexts;

    public interface IGraphicsAdapter
    {
        GraphicsBackend Backend { get; }

        IGraphicsDevice CreateGraphicsDevice(bool debug);

        void PumpDebugMessages();

        int PlatformScore { get; }
    }
}