namespace Hexa.NET.KittyUI.Graphics
{
    public enum GraphicsBackend
    {
        /// <summary>
        /// Automatically choose the backend based on OS (For Windows it's D3D11, for others OpenGL 4.5 core)
        /// </summary>
        Auto,

        /// <summary>
        /// DirectX 11 backend. (Windows only)
        /// </summary>
        D3D11,

        /// <summary>
        /// OpenGL 4.5 core backend. (All platforms)
        /// On macOS OpenGL 4.1 core is used.
        /// </summary>
        OpenGL,

        /// <summary>
        /// OpenGLES 3 backend. (Android)
        /// </summary>
        OpenGLES,

        /// <summary>
        /// Not supported yet.
        /// </summary>
        Vulkan,

        /// <summary>
        /// Not supported yet.
        /// </summary>
        Metal
    }
}