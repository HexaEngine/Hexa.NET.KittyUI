namespace Kitty.Graphics
{
    using Kitty.Mathematics;

    public interface IRenderTargetView : IDeviceChild
    {
        RenderTargetViewDescription Description { get; }

        public Viewport Viewport { get; }
    }
}