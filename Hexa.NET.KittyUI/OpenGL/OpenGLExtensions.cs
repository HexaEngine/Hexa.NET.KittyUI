namespace Hexa.NET.KittyUI.OpenGL
{
    using Hexa.NET.OpenGL;

    public static class OpenGLExtensions
    {
        public static void GLCheckError(GL GL)
        {
            var error = (GLErrorCode)GL.GetError();
            if (error != GLErrorCode.NoError)
            {
                throw new Exception($"OpenGL error: {error}");
            }
        }
    }
}