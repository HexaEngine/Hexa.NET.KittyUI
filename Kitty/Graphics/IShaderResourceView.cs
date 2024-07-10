namespace Kitty.Graphics
{
    public interface IShaderResourceView : IDeviceChild
    {
        ShaderResourceViewDescription Description { get; }
    }
}