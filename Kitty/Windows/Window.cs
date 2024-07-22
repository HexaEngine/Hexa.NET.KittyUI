namespace Kitty.Windows
{
    using Kitty;
    using Kitty.Audio;
    using Kitty.Debugging;
    using Kitty.Graphics;
    using Kitty.ImGuiBackend;
    using Kitty.Mathematics;
    using Kitty.Threading;
    using Kitty.UI;
    using Kitty.Windows.Events;
    using System;
    using System.Numerics;

    public class Window : SdlWindow, IRenderWindow
    {
#nullable disable
        private ThreadDispatcher renderDispatcher;
        private bool firstFrame;
        private IAudioDevice audioDevice;
        private IGraphicsDevice graphicsDevice;
        private IGraphicsContext graphicsContext;
        private ISwapChain swapChain;
#nullable restore
        private bool resize = false;
        private ImGuiManager? imGuiRenderer;

        public IThreadDispatcher Dispatcher => renderDispatcher;

        public IGraphicsDevice Device => graphicsDevice;

        public IGraphicsContext Context => graphicsContext;

        public IAudioDevice AudioDevice => audioDevice;

        public ISwapChain SwapChain => swapChain;

        public Viewport RenderViewport => default;

        public event Action<IGraphicsContext>? Draw;

        public Window()
        {
        }

        public virtual void Initialize(AppBuilder appBuilder, IAudioDevice audioDevice, IGraphicsDevice graphicsDevice)
        {
            this.audioDevice = audioDevice;
            this.graphicsDevice = graphicsDevice;
            graphicsContext = graphicsDevice.Context;
            swapChain = graphicsDevice.CreateSwapChain(this) ?? throw new PlatformNotSupportedException();
            swapChain.Active = true;
            swapChain.LimitFPS = false;
            swapChain.VSync = true;
            renderDispatcher = new(Thread.CurrentThread);

            if (Application.MainWindow == this)
            {
                AudioManager.Initialize(audioDevice);
                PipelineManager.Initialize(graphicsDevice);
            }

            imGuiRenderer = new(this, appBuilder, graphicsDevice, graphicsContext);

            WidgetManager.Init(graphicsDevice);

            OnRendererInitialize(graphicsDevice);
        }

        public void Render(IGraphicsContext context)
        {
            if (resize)
            {
                swapChain.Resize(Width, Height);
                resize = false;
            }

            if (firstFrame)
            {
                Time.Initialize();
                firstFrame = false;
            }

            context.ClearDepthStencilView(swapChain.BackbufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            context.ClearRenderTargetView(swapChain.BackbufferRTV, Vector4.Zero);

            renderDispatcher.ExecuteQueue();

            imGuiRenderer?.NewFrame();

            OnRenderBegin(context);

            WidgetManager.Draw(context);
            ImGuiConsole.Draw();
            MessageBoxes.Draw();

            OnRender(context);

            context.SetRenderTarget(swapChain.BackbufferRTV, null);
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
            swapChain.Dispose();
            graphicsContext.Dispose();
            graphicsDevice.Dispose();
        }

        protected virtual void OnRendererInitialize(IGraphicsDevice device)
        {
        }

        protected virtual void OnRenderBegin(IGraphicsContext context)
        {
        }

        protected virtual void OnRender(IGraphicsContext context)
        {
            Draw?.Invoke(context);
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