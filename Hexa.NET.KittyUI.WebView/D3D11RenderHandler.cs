namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Enums;
    using CefSharp.Structs;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.OpenGL;
    using HexaGen.Runtime.COM;

    public unsafe class D3D11RenderHandler : RenderHandlerBase
    {
        private ComPtr<ID3D11Device5> device;
        private ComPtr<ID3D11DeviceContext3> deviceContext;
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> srv;

        public D3D11RenderHandler()
        {
            device = D3D11GraphicsDevice.Device;
            deviceContext = D3D11GraphicsDevice.DeviceContext;
        }

        public override void Draw(ImDrawListPtr draw, ImRect rect)
        {
            while (drawQueue.TryDequeue(out CefDrawData result))
            {
                if (result.Type == PaintElementType.View)
                {
                    deviceContext.UpdateSubresource(texture.As<ID3D11Resource>(), 0, (Box*)null, result.Data, (uint)(result.Width * 4), 0);
                }

                result.Release();
            }

            if (requestedCursor != ImGuiMouseCursor.None)
            {
                ImGui.SetMouseCursor(requestedCursor);
            }

            draw.AddImage((ulong)srv.Handle, rect.Min, rect.Max);
        }

        protected override void SetBufferSize(int width, int height)
        {
            if (srv.Handle != null)
            {
                srv.Release();
                srv = default;
            }

            if (texture.Handle != null)
            {
                texture.Release();
                texture = default;
            }

            Texture2DDesc textureDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                ArraySize = 1,
                Format = Format.B8G8R8A8Unorm,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
                BindFlags = (uint)BindFlag.ShaderResource,
                Usage = Usage.Default,
                SampleDesc = new SampleDesc(1, 0),
                MipLevels = 1
            };

            device.CreateTexture2D(ref textureDesc, (SubresourceData*)null, out texture).ThrowIf();
            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), (ShaderResourceViewDesc*)null, out srv).ThrowIf();
        }

        public override bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        public override void UpdateDragCursor(DragOperationsMask operation)
        {
        }

        public override void OnPopupShow(bool show)
        {
        }

        public override void OnPopupSize(Rect rect)
        {
        }

        protected override void DisposeCore()
        {
            if (srv.Handle != null)
            {
                srv.Release();
                srv = default;
            }

            if (texture.Handle != null)
            {
                texture.Release();
                texture = default;
            }
        }
    }
}