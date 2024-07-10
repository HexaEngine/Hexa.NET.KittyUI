namespace Kitty.Graphics
{
    public interface IUnorderedAccessView : IDeviceChild
    {
        public UnorderedAccessViewDescription Description { get; }
    }
}