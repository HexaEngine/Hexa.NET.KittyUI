namespace Kitty.Audio
{
    public interface IAudioContext : IDisposable
    {
        public nint NativePointer { get; }
    }
}