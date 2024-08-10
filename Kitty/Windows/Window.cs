namespace Hexa.NET.Kitty.Windows
{
    using Hexa.NET.Kitty;
    using Hexa.NET.Kitty.Audio;
    using Hexa.NET.Kitty.D3D11;
    using Hexa.NET.Kitty.Debugging;
    using Hexa.NET.Kitty.ImGuiBackend;
    using Hexa.NET.Kitty.Threading;
    using Hexa.NET.Kitty.UI;
    using Hexa.NET.Kitty.UI.Dialogs;
    using Hexa.NET.Kitty.Windows.Events;
    using Kitty.OpenGL;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.OpenGL;
    using System;
    using System.Numerics;

    public class Window : SdlWindow, IRenderWindow
    {
#nullable disable
        private ThreadDispatcher renderDispatcher;
        private bool firstFrame;
        private IAudioDevice audioDevice;
#nullable restore
        private bool resize = false;
        private ImGuiManager? imGuiRenderer;
        private DXGISwapChain swapChain;
        private GL gl;
        private IGLContext glContext;

        public IThreadDispatcher Dispatcher => renderDispatcher;

        public IAudioDevice AudioDevice => audioDevice;

        public event Action? Draw;

        public Window()
        {
        }

        public virtual unsafe void Initialize(AppBuilder appBuilder, IAudioDevice audioDevice)
        {
            this.audioDevice = audioDevice;

            renderDispatcher = new(Thread.CurrentThread);

            if (Application.MainWindow == this)
            {
                AudioManager.Initialize(audioDevice);
            }

            imGuiRenderer = new(appBuilder);

            WidgetManager.Init();

            OnRendererInitialize();

            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    swapChain = D3D11Adapter.CreateSwapChainForWindow(this);
                    swapChain.Active = true;
                    swapChain.LimitFPS = false;
                    swapChain.VSync = true;
                    var dev = D3D11GraphicsDevice.Device;
                    var ctx = D3D11GraphicsDevice.DeviceContext;
                    ImGuiSDL2Platform.InitForD3D(GetWindow());
                    ImGuiD3D11Renderer.Init(*(ComPtr<ID3D11Device>*)&dev, *(ComPtr<ID3D11DeviceContext>*)&ctx);
                    imGuiRenderer.RenderDrawData = (data) => ImGuiD3D11Renderer.RenderDrawData(data);
                    break;

                case GraphicsBackend.OpenGL:
                    gl = OpenGLAdapter.GL;
                    glContext = OpenGLAdapter.Context;
                    glContext.SwapInterval(1);
                    ImGuiSDL2Platform.InitForOpenGL(GetWindow(), (void*)glContext.Handle);
                    ImGuiOpenGL3Renderer.Init(gl, null);
                    imGuiRenderer.RenderDrawData = (data) => ImGuiOpenGL3Renderer.RenderDrawData(data);
                    break;
            }
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

                default:
                    break;
            }
        }

        private void RenderOpenGL()
        {
            glContext.MakeCurrent();
            if (resize)
            {
                gl.Viewport(0, 0, (uint)Width, (uint)Height);
            }

            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            renderDispatcher.ExecuteQueue();

            imGuiRenderer?.NewFrame();

            OnRenderBegin();

            WidgetManager.Draw();
            DialogManager.Draw();
            ImGuiConsole.Draw();
            MessageBoxes.Draw();

            OnRender();

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            imGuiRenderer?.EndFrame();

            glContext.MakeCurrent();
            glContext.SwapBuffers();
        }

        private unsafe void RenderD3D11()
        {
            if (resize)
            {
                swapChain.Resize(Width, Height);
                resize = false;
            }

            var context = D3D11GraphicsDevice.DeviceContext;
            var color = Vector4.Zero;
            context.ClearRenderTargetView(swapChain.BackbufferRTV, (float*)&color);

            renderDispatcher.ExecuteQueue();

            imGuiRenderer?.NewFrame();

            OnRenderBegin();

            WidgetManager.Draw();
            DialogManager.Draw();
            ImGuiConsole.Draw();
            MessageBoxes.Draw();

            OnRender();

            var rtv = swapChain.BackbufferRTV.Handle;
            context.OMSetRenderTargets(1, ref rtv, (ID3D11DepthStencilView*)null);
            imGuiRenderer?.EndFrame();

            swapChain.Present();

            swapChain.Wait();
        }

        public virtual void Uninitialize()
        {
            OnRendererDispose();

            WidgetManager.Dispose();

            imGuiRenderer?.Dispose();

            renderDispatcher.Dispose();
            AudioManager.Release();

            switch (Backend)
            {
                case GraphicsBackend.D3D11:
                    ImGuiD3D11Renderer.Shutdown();
                    swapChain.Dispose();
                    break;

                case GraphicsBackend.OpenGL:
                    ImGuiOpenGL3Renderer.Shutdown();
                    break;
            }
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
        }
    }
}