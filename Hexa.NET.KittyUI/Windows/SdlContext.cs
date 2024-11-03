namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;

    public unsafe class SdlContext : IGLContext
    {
        private SDLGLContext _ctx;
        private SDLWindow* _window;

        /// <summary>
        /// Creates a <see cref="SdlContext"/> from a native window using the given native interface.
        /// </summary>
        /// <param name="window">The native window to associate this context for.</param>
        public SdlContext(SDLWindow* window)
        {
            Window = window;
            _ctx = SDL.GLCreateContext(window);
        }

        /// <summary>
        /// The native window to create a context for.
        /// </summary>
        public SDLWindow* Window
        {
            get => _window;
            set
            {
                _window = value;
            }
        }

        public Point2 FramebufferSize
        {
            get
            {
                var ret = stackalloc int[2];
                SDL.GLGetDrawableSize(Window, ret, &ret[1]);
                return *(Point2*)ret;
            }
        }

        public void Dispose()
        {
            if (_ctx != default)
            {
                SDL.GLDeleteContext(_ctx);
                _ctx = default;
            }
        }

        public nint GetProcAddress(string proc)
        {
            SDL.ClearError();
            var ret = (nint)SDL.GLGetProcAddress(proc);
            if (ret == 0)
            {
                throw new EntryPointNotFoundException(proc);
            }

            return ret;
        }

        public bool TryGetProcAddress(string proc, out nint addr)
        {
            addr = 0;
            SDL.ClearError();
            if (_ctx == default)
            {
                return false;
            }

            var ret = (nint)SDL.GLGetProcAddress(proc);
            if (!string.IsNullOrWhiteSpace(SDL.GetErrorS()))
            {
                SDL.ClearError();
                return false;
            }

            return (addr = ret) != 0;
        }

        public nint Handle
        {
            get
            {
                return _ctx.Handle;
            }
        }

        public bool IsCurrent
        {
            get
            {
                return SDL.GLGetCurrentContext() == _ctx;
            }
        }

        public void SwapInterval(int interval)
        {
            SDL.GLSetSwapInterval(interval);
        }

        public void SwapBuffers()
        {
            SDL.GLSwapWindow(Window);
        }

        public void MakeCurrent()
        {
            SDL.GLMakeCurrent(Window, _ctx);
        }

        public bool IsExtensionSupported(string extensionName)
        {
            return SDL.GLExtensionSupported(extensionName) == SDLBool.True;
        }
    }
}