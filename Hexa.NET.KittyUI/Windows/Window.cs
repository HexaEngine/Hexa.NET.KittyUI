namespace Hexa.NET.KittyUI.Windows
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Backends.D3D11;
    using Hexa.NET.ImGui.Backends.OpenGL3;
    using Hexa.NET.ImGui.Backends.SDL2;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI;
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Input.Events;
    using Hexa.NET.KittyUI.OpenGL;
    using Hexa.NET.KittyUI.Threading;
    using Hexa.NET.KittyUI.Windows.Events;
    using System;
    using System.Numerics;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.D3D11;
    using Hexa.NET.Logging;
    using HexaGen.Runtime;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public class Window : CoreWindow, IRenderWindow
    {
#nullable disable
        private ThreadDispatcher renderDispatcher;

#nullable restore
        private bool resize = false;
        private bool showDebugTools = false;
        private bool limitFPS;

        protected ImGuiManager? imGuiRenderer;
        protected FPSLimiter limiter = new();

        public IThreadDispatcher Dispatcher => renderDispatcher;

        public event Action? Draw;

        public bool LimitFPS { get => limitFPS; set => limitFPS = value; }

        public int TargetFPS
        {
            get => limiter.TargetFPS; set => limiter.TargetFPS = value;
        }

        public virtual unsafe void Initialize(AppBuilder appBuilder)
        {
            renderDispatcher = new(Thread.CurrentThread);

            ImGuiContextPtr context;
            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    if (!OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException("Direct3D 11 is only supported on Windows.");
                    }

                    var dev = D3D11GraphicsDevice.Device;
                    var ctx = D3D11GraphicsDevice.DeviceContext;
                    imGuiRenderer = new(appBuilder, ImGuiImplD3D11.NewFrame, ImGuiImplD3D11.RenderDrawData);
                    context = ImGui.GetCurrentContext();
                    ImGuiImplSDL2.SetCurrentContext(context);
                    ImGuiImplSDL2.InitForD3D((SDLWindow*)GetWindow());
                    ImGuiImplD3D11.SetCurrentContext(context);
                    ImGuiImplD3D11.Init((NET.ImGui.Backends.D3D11.ID3D11Device*)dev.Handle, (NET.ImGui.Backends.D3D11.ID3D11DeviceContext*)ctx.Handle);
                    break;

                case GraphicsBackend.OpenGL:
                    imGuiRenderer = new(appBuilder, ImGuiImplOpenGL3.NewFrame, ImGuiImplOpenGL3.RenderDrawData);
                    context = ImGui.GetCurrentContext();
                    ImGuiImplSDL2.SetCurrentContext(context);
                    ImGuiImplSDL2.InitForOpenGL((SDLWindow*)GetWindow(), (void*)GLContext.Handle);
                    ImGuiImplOpenGL3.SetCurrentContext(context);
                    ImGuiImplOpenGL3.Init((byte*)null);
                    break;
            }

            Application.RegisterHook(SDLEventHook);

            WidgetManager.Init();

            OnRendererInitialize();
        }

        private unsafe bool SDLEventHook(SDL2.SDLEvent evnt)
        {
            return ImGuiImplSDL2.ProcessEvent((SDLEvent*)&evnt);
        }

        public virtual void Render()
        {
            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    RenderD3D11();
                    break;

                case GraphicsBackend.OpenGL:
                    RenderOpenGL();
                    break;

                default:
                    break;
            }
        }

        private readonly List<Key> keystack = new();

        protected override void OnKeyboardInput(KeyboardEventArgs args)
        {
            if (args.State == KeyState.Down)
                keystack.Add(args.KeyCode);
            else
                keystack.Remove(args.KeyCode);
            if (keystack.Count == 3)
            {
                if (keystack[0] == Key.LCtrl && keystack[1] == Key.LShift && keystack[2] == Key.D)
                {
                    ToggleDebugTools();
                }
                keystack.Clear();
            }
            base.OnKeyboardInput(args);
        }

        public void ToggleDebugTools()
        {
            showDebugTools = !showDebugTools;

            ImGuiDebugTools.Shown = showDebugTools;
        }

        protected virtual void RenderOpenGL()
        {
            GLContext!.MakeCurrent();

            if (resize)
            {
                GL!.Viewport(0, 0, Width, Height);
            }

            GL!.Clear(GLClearBufferMask.ColorBufferBit);

            renderDispatcher.ExecuteQueue();

            imGuiRenderer?.NewFrame();

            OnRenderBegin();

            TitleBar?.Draw();
            WidgetManager.Draw();
            ImGuiDebugTools.Draw();

            OnRender();

            GL.BindFramebuffer(GLFramebufferTarget.Framebuffer, 0);
            imGuiRenderer?.EndFrame();

            GLContext.MakeCurrent();
            GLContext.SwapBuffers();

            OpenGLAdapter.ProcessQueues(); // Process all pending uploads

            if (limitFPS)
            {
                limiter.LimitFrameRate();
            }
        }

        protected virtual unsafe void RenderD3D11()
        {
            if (resize)
            {
                DXGISwapChain!.Resize(Width, Height);
                resize = false;
            }

            var context = D3D11GraphicsDevice.DeviceContext;
            if (context.Handle == null)
            {
                return;
            }
            var color = Vector4.Zero;
            context.ClearRenderTargetView(DXGISwapChain!.BackbufferRTV, (float*)&color);

            renderDispatcher.ExecuteQueue();

            imGuiRenderer?.NewFrame();

            OnRenderBegin();

            TitleBar?.Draw();
            WidgetManager.Draw();
            ImGuiDebugTools.Draw();

            OnRender();

            var rtv = DXGISwapChain.BackbufferRTV.Handle;
            context.OMSetRenderTargets(1, ref rtv, (ID3D11DepthStencilView*)null);
            imGuiRenderer?.EndFrame();

            DXGISwapChain.Present();

            if (limitFPS)
            {
                limiter.LimitFrameRate();
            }
        }

        public virtual void Uninitialize()
        {
            Application.UnregisterHook(SDLEventHook);
            OnRendererDispose();

            WidgetManager.Dispose();
            renderDispatcher.Dispose();
          

            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    ImGuiImplD3D11.Shutdown();
                    break;

                case GraphicsBackend.OpenGL:
                    ImGuiImplOpenGL3.Shutdown();
                    break;
            }

            ImGuiImplSDL2.Shutdown();

            imGuiRenderer?.Dispose();
        }

        protected virtual void OnRendererInitialize()
        {
        }

        protected virtual void OnRenderBegin()
        {
        }

        protected virtual void OnRender()
        {
            Draw?.Invoke();
        }

        protected virtual void OnRendererDispose()
        {
        }

        protected override void OnResized(ResizedEventArgs args)
        {
            resize = true;
            base.OnResized(args);
            LoggerFactory.General.Info($"Resized: {args.NewWidth}x{args.NewHeight}");
        }
    }
}