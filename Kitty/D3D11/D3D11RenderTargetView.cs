using Hexa.NET.Mathematics;

namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Direct3D11;
    using System;
    using Viewport = Viewport;

    public unsafe class D3D11RenderTargetView : DeviceChildBase, IRenderTargetView
    {
        internal readonly ID3D11RenderTargetView* rtv;

        public D3D11RenderTargetView(ID3D11RenderTargetView* rtv, RenderTargetViewDescription description, Viewport viewport)
        {
            this.rtv = rtv;
            nativePointer = new(rtv);
            Description = description;
            Viewport = viewport;
        }

        public RenderTargetViewDescription Description { get; }

        public Viewport Viewport { get; }

        protected override void DisposeCore()
        {
            rtv->Release();
        }
    }
}