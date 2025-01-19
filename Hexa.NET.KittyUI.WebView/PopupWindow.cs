namespace Hexa.NET.KittyUI.WebView
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.SDL2;
    using Hexa.NET.KittyUI.Graphics;

    public unsafe class PopupWindow : CoreWindow
    {
        protected FPSLimiter limiter = new();
        private bool limitFPS;
      
        public PopupWindow() : base(SDLWindowFlags.Hidden | SDLWindowFlags.Borderless)
        {
        }

        public bool LimitFPS { get => limitFPS; set => limitFPS = value; }

        public int TargetFPS
        {
            get => limiter.TargetFPS; set => limiter.TargetFPS = value;
        }

        public void Initialize()
        {
            switch (Backend)
            {
                case GraphicsBackend.D3D11:              
                    break;

                case GraphicsBackend.OpenGL:
                    break;
            }
        }

        public void ShowPopup(int x, int y, int width, int height)
        {
            Position = new(x, y);
            Size = new(width, height);
            Show();
        }

        public void HidePopup()
        {
            Hide();
        }

        public void Render()
        {
            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    RenderD3D11();
                    break;

                case GraphicsBackend.OpenGL:
                    RenderOpenGL();
                    break;
            }
        }

        private void RenderD3D11()
        {
            DXGISwapChain!.Present();

            if (limitFPS)
            {
                limiter.LimitFrameRate();
            }
        }

        private void RenderOpenGL()
        {
            GLContext!.MakeCurrent();
            GLContext.SwapBuffers();

            if (limitFPS)
            {
                limiter.LimitFrameRate();
            }
        }
    }
}