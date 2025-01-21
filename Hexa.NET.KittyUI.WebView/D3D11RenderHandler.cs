namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Enums;
    using CefSharp.Structs;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.D3D11;
    using HexaGen.Runtime.COM;

    public unsafe class D3D11RenderHandler : RenderHandlerBase
    {
        private ComPtr<ID3D11Device5> device;
        private ComPtr<ID3D11DeviceContext3> deviceContext;
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private int width;
        private int height;

        public D3D11RenderHandler()
        {
            device = D3D11GraphicsDevice.Device;
            deviceContext = D3D11GraphicsDevice.DeviceContext;
        }

        public override void Draw(ImDrawListPtr draw, ImRect rect, bool hovered)
        {
            while (drawQueue.TryDequeue(out CefDrawData result))
            {
                if (result.Type == PaintElementType.View)
                {
                    if (srv.Handle == null || result.Width != width || result.Height != height)
                    {
                        Resize(result);
                    }
                    else
                    {
                        uint left = (uint)result.DirtyRect.X;
                        uint top = (uint)result.DirtyRect.Y;
                        uint right = left + (uint)result.DirtyRect.Width;
                        uint bottom = top + (uint)result.DirtyRect.Height;
                        Box box = new(left, top, 0, right, bottom, 1);
                        const uint stride = 4;
                        uint rowPitch = stride * (uint)result.Width;
                        byte* data = (byte*)result.Data + (top * rowPitch + left * stride);
                        deviceContext.UpdateSubresource1(texture.As<ID3D11Resource>(), 0, &box, data, (uint)(result.Width * 4), 0, (uint)CopyFlags.Discard);
                    }
                }

                result.Release();
            }

            if (hovered && requestedCursor != ImGuiMouseCursor.None)
            {
                ImGui.SetMouseCursor(requestedCursor);
            }

            draw.AddImage((ulong)srv.Handle, rect.Min, rect.Max);
        }

        private void Resize(CefDrawData result)
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
                Width = (uint)result.Width,
                Height = (uint)result.Height,
                ArraySize = 1,
                Format = Format.B8G8R8A8Unorm,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
                BindFlags = (uint)BindFlag.ShaderResource,
                Usage = Usage.Default,
                SampleDesc = new SampleDesc(1, 0),
                MipLevels = 1
            };

            width = result.Width;
            height = result.Height;

            SubresourceData data = new(result.Data, (uint)result.Width * 4);
            device.CreateTexture2D(ref textureDesc, &data, out texture).ThrowIf();
            device.CreateShaderResourceView(texture.As<ID3D11Resource>(), (ShaderResourceViewDesc*)null, out srv).ThrowIf();
        }

        protected override void SetBufferSize(int oldWidth, int oldHeight, int width, int height)
        {
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