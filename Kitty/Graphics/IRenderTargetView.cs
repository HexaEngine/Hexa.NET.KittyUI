namespace Kitty.Graphics
{
    using Hexa.NET.Mathematics;

    public interface IRenderTargetView : IDeviceChild
    {
        RenderTargetViewDescription Description { get; }

        public Viewport Viewport { get; }
    }
}