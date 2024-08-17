namespace Hexa.NET.KittyUI.OpenGL
{
    using Silk.NET.OpenGL;

    public static class OpenGLExtensions
    {
        public static void CheckError(this GL gl)
        {
            var error = gl.GetError();
            if (error != GLEnum.NoError)
            {
                throw new Exception($"OpenGL error: {error}");
            }
        }
    }
}