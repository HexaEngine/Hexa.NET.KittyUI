namespace Kitty.Graphics
{
    public interface IDepthStencilView : IDeviceChild
    {
        DepthStencilViewDescription Description { get; }
    }
}