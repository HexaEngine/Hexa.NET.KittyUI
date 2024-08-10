namespace Hexa.NET.Kitty.Audio
{
    using Hexa.NET.Mathematics;
    using System.Numerics;

    public interface IListener : IDisposable
    {
        bool IsActive { get; set; }
        AudioOrientation Orientation { get; set; }
        Vector3 Position { get; set; }
        Vector3 Velocity { get; set; }
    }
}