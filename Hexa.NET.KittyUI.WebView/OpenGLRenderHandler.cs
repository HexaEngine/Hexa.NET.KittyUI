namespace Hexa.NET.KittyUI.WebView
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using CefSharp;
    using CefSharp.Enums;
    using CefSharp.Structs;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.OpenGL;
    using HexaGen.Runtime;

    public unsafe class OpenGLRenderHandler : RenderHandlerBase
    {
        private readonly GL GL;
        private readonly IGLContext context;
        private uint textureId;
        private int width;
        private int height;

        public OpenGLRenderHandler()
        {
            GL = OpenGLAdapter.GL;
            context = OpenGLAdapter.Context;
        }

        public override void Draw(ImDrawListPtr draw, ImRect bb, bool hovered)
        {
            while (drawQueue.TryDequeue(out CefDrawData result))
            {
                if (result.Type == PaintElementType.View)
                {
                    if (textureId == 0)
                    {
                        textureId = GL.GenTexture();
                        GL.BindTexture(GLTextureTarget.Texture2D, textureId);
                        GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)GLTextureMinFilter.Nearest);
                        GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)GLTextureMinFilter.Nearest);
                        GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)GLTextureWrapMode.ClampToEdge);
                        GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)GLTextureWrapMode.ClampToEdge);
                    }
                    else
                    {
                        GL.BindTexture(GLTextureTarget.Texture2D, textureId);
                    }
                    
                    GL.PixelStorei(GLPixelStoreParameter.UnpackRowLength, result.Width);
                    if (result.Width != width || result.Height != height)
                    {
                        GL.PixelStorei(GLPixelStoreParameter.UnpackSkipPixels, 0);
                        GL.PixelStorei(GLPixelStoreParameter.UnpackSkipRows, 0);
                        GL.TexImage2D(GLTextureTarget.Texture2D, 0, GLInternalFormat.Rgba, result.Width, result.Height, 0, GLPixelFormat.Bgra, GLPixelType.UnsignedInt8888Rev, result.Data);
                        width = result.Width;
                        height = result.Height;
                    }
                    else
                    {
                        GL.PixelStorei(GLPixelStoreParameter.UnpackSkipPixels, result.DirtyRect.X);
                        GL.PixelStorei(GLPixelStoreParameter.UnpackSkipRows, result.DirtyRect.Y);
                        GL.TexSubImage2D(GLTextureTarget.Texture2D, 0, result.DirtyRect.X, result.DirtyRect.Y, result.DirtyRect.Width, result.DirtyRect.Height, GLPixelFormat.Bgra, GLPixelType.UnsignedInt8888Rev, result.Data);
                    }
                    GL.BindTexture(GLTextureTarget.Texture2D, 0);
                }

                result.Release();
            }

            if (hovered && requestedCursor != ImGuiMouseCursor.None)
            {
                ImGui.SetMouseCursor(requestedCursor);
            }

            draw.AddImage(textureId, bb.Min, bb.Max);
        }

        protected override void SetBufferSize(int oldWidth, int oldHeight, int width, int height)
        {
        }

        public override void OnPopupShow(bool show)
        {
        }

        public override void OnPopupSize(Rect rect)
        {
        }

        public override bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        public override void UpdateDragCursor(DragOperationsMask operation)
        {
        }

        protected override void DisposeCore()
        {
            if (textureId != 0)
            {
                GL.DeleteTexture(textureId);
                textureId = 0;
            }
        }
    }
}