namespace Kitty.Graphics
{
    public interface ISamplerState : IDeviceChild
    {
        public SamplerDescription Description { get; }
    }
}