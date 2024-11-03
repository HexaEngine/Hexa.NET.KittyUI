namespace Hexa.NET.KittyUI.D3D11
{
    using Hexa.NET.KittyUI.Windows.Events;
    using HexaGen.Runtime;
    using HexaGen.Runtime.COM;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public unsafe class DXGISwapChain : DeviceChildBase
    {
        private ComPtr<IDXGISwapChain1> swapChain;
        private readonly SwapChainFlag flags;
        private ComPtr<ID3D11Texture2D> backbuffer;
        private long fpsStartTime;
        private long fpsFrameCount;
        private bool vSync;
        private bool limitFPS;
        private int targetFPS = 120;
        private bool active;
        private ComPtr<ID3D11RenderTargetView> backbufferRTV;

        internal DXGISwapChain(ComPtr<IDXGISwapChain1> swapChain, SwapChainFlag flags)
        {
            this.swapChain = swapChain;
            this.flags = flags;

            swapChain.GetBuffer(0, out backbuffer);
            Texture2DDesc desc;
            backbuffer.GetDesc(&desc);

            var dev = D3D11GraphicsDevice.Device;

            dev.CreateRenderTargetView(backbuffer.As<ID3D11Resource>(), null, out backbufferRTV);

            Width = (int)desc.Width;
            Height = (int)desc.Height;
            Viewport = new(0, 0, Width, Height);
        }

        public ComPtr<ID3D11Texture2D> Backbuffer => backbuffer;

        public ComPtr<ID3D11RenderTargetView> BackbufferRTV { get => backbufferRTV; private set => backbufferRTV = value; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Mathematics.Viewport Viewport { get; private set; }

        public event EventHandler? Resizing;

        public event EventHandler<ResizedEventArgs>? Resized;

        public bool VSync { get => vSync; set => vSync = value; }

        public bool LimitFPS { get => limitFPS; set => limitFPS = value; }

        public int TargetFPS { get => targetFPS; set => targetFPS = value; }

        public bool Active { get => active; set => active = value; }

        public void Present(bool sync)
        {
            if (sync)
            {
                swapChain.Present(1, 0);
            }
            else
            {
                swapChain.Present(0, (uint)DXGI.DXGI_PRESENT_ALLOW_TEARING);
            }
        }

        public void Present()
        {
            if (!active)
            {
                swapChain.Present(4, 0);
            }
            else if (vSync)
            {
                swapChain.Present(1, 0);
            }
            else
            {
                swapChain.Present(0, (uint)DXGI.DXGI_PRESENT_ALLOW_TEARING);
            }
        }

        public void Wait()
        {
            if (!vSync && limitFPS)
            {
                LimitFrameRate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LimitFrameRate()
        {
            int fps = targetFPS;
            long freq = Stopwatch.Frequency;
            long frame = Stopwatch.GetTimestamp();
            while ((frame - fpsStartTime) * fps < freq * fpsFrameCount)
            {
                int sleepTime = (int)((fpsStartTime * fps + freq * fpsFrameCount - frame * fps) * 1000 / (freq * fps));
                if (sleepTime > 0) Thread.Sleep(sleepTime);
                frame = Stopwatch.GetTimestamp();
            }
            if (++fpsFrameCount > fps)
            {
                fpsFrameCount = 0;
                fpsStartTime = frame;
            }
        }

        public void Resize(int width, int height)
        {
            var oldWidth = Width;
            var oldHeight = Height;
            Resizing?.Invoke(this, EventArgs.Empty);

            Backbuffer.Dispose();
            BackbufferRTV.Dispose();

            swapChain.ResizeBuffers(2, (uint)width, (uint)height, Format.B8G8R8A8Unorm, (uint)flags);
            Width = width;
            Height = height;
            Viewport = new(0, 0, Width, Height);

            swapChain.GetBuffer(0, out backbuffer);
            Texture2DDesc desc;
            backbuffer.GetDesc(&desc);

            var dev = D3D11GraphicsDevice.Device;

            dev.CreateRenderTargetView(backbuffer.As<ID3D11Resource>(), null, out backbufferRTV);

            Resized?.Invoke(this, new(oldWidth, oldHeight, width, height));
        }

        protected override void DisposeCore()
        {
            Backbuffer.Dispose();
            BackbufferRTV.Dispose();
            swapChain.Release();
        }
    }
}