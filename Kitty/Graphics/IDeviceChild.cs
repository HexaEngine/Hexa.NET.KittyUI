namespace Kitty.Graphics
{
    using Hexa.NET.Utilities;
    using System.Runtime.CompilerServices;

    public interface IDeviceChild : INative
    {
        string? DebugName { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; [MethodImpl(MethodImplOptions.AggressiveInlining)] set; }

        bool IsDisposed { get; }

        event EventHandler? OnDisposed;
    }
}