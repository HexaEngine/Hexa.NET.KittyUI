namespace Kitty.Graphics
{
    using Kitty.Mathematics;

    public interface IView
    {
        public CameraTransform Transform { get; }
    }
}