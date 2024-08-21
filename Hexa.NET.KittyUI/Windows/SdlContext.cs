namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.SDL2;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Loader;
    using Silk.NET.Maths;
    using System;

    public unsafe class SdlContext : IGLContext
    {
        private SDLGLContext _ctx;
        private SDLWindow* _window;

        /// <summary>
        /// Creates a <see cref="SdlContext"/> from a native window using the given native interface.
        /// </summary>
        /// <param name="window">The native window to associate this context for.</param>
        /// <param name="source">The <see cref="IGLContextSource" /> to associate this context to, if any.</param>
        /// <param name="attributes">The attributes to eagerly pass to <see cref="Create"/>.</param>
        public SdlContext(
            SDLWindow* window,
            IGLContextSource? source = null,
            params (SDLGLattr Attribute, int Value)[] attributes)
        {
            Window = window;
            Source = source;
            if (attributes is not null && attributes.Length > 0)
            {
                Create(attributes);
            }
        }

        /// <summary>
        /// The native window to create a context for.
        /// </summary>
        public SDLWindow* Window
        {
            get => _window;
            set
            {
                AssertNotCreated();
                _window = value;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public Vector2D<int> FramebufferSize
        {
            get
            {
                AssertCreated();
                var ret = stackalloc int[2];
                SDL.GLGetDrawableSize(Window, ret, &ret[1]);
                //SDL.ThrowError();
                return *(Vector2D<int>*)ret;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void Create(params (SDLGLattr Attribute, int Value)[] attributes)
        {
            foreach (var (attribute, value) in attributes)
            {
                if (SDL.GLSetAttribute(attribute, value) != 0)
                {
                    //SDL.ThrowError();
                }
            }

            _ctx = SDL.GLCreateContext(Window);
            if (_ctx == default)
            {
                //SDL.ThrowError();
            }
        }

        private void AssertCreated()
        {
            if (_ctx == default)
            {
                throw new InvalidOperationException("Context not created.");
            }
        }

        private void AssertNotCreated()
        {
            if (_ctx != default)
            {
                throw new InvalidOperationException("Context created already.");
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void Dispose()
        {
            if (_ctx != default)
            {
                SDL.GLDeleteContext(_ctx);
                _ctx = default;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public nint GetProcAddress(string proc, int? slot = default)
        {
            AssertCreated();
            SDL.ClearError();
            var ret = (nint)SDL.GLGetProcAddress(proc);
            //SDL.ThrowError();
            if (ret == 0)
            {
                Throw(proc);
                return 0;
            }

            return ret;
            static void Throw(string proc) => throw new SymbolLoadingException(proc);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = default)
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

        /// <inheritdoc cref="IGLContext" />
        public nint Handle
        {
            get
            {
                AssertCreated();
                return _ctx.Handle;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public IGLContextSource? Source { get; }

        /// <inheritdoc cref="IGLContext" />
        public bool IsCurrent
        {
            get
            {
                AssertCreated();
                return SDL.GLGetCurrentContext() == _ctx;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void SwapInterval(int interval)
        {
            AssertCreated();
            SDL.GLSetSwapInterval(interval);
        }

        /// <inheritdoc cref="IGLContext" />
        public void SwapBuffers()
        {
            AssertCreated();
            SDL.GLSwapWindow(Window);
        }

        /// <inheritdoc cref="IGLContext" />
        public void MakeCurrent()
        {
            AssertCreated();
            SDL.GLMakeCurrent(Window, _ctx);
        }

        /// <inheritdoc cref="IGLContext" />
        public void Clear()
        {
            AssertCreated();
            if (IsCurrent)
            {
                SDL.GLMakeCurrent(Window, default);
            }
        }
    }
}