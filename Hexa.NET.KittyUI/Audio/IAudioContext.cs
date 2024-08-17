namespace Hexa.NET.KittyUI.Audio
{
    public interface IAudioContext : IDisposable
    {
        public nint NativePointer { get; }
    }
}