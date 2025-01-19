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

        public OpenGLRenderHandler()
        {
            GL = OpenGLAdapter.GL;
            context = OpenGLAdapter.Context;
        }

        public override void Draw(ImDrawListPtr draw, ImRect bb)
        {
            while (drawQueue.TryDequeue(out CefDrawData result))
            {
                if (result.Type == PaintElementType.View)
                {
                    GL.BindTexture(GLTextureTarget.Texture2D, textureId);
                    GL.TexSubImage2D(GLTextureTarget.Texture2D, 0, 0, 0, result.Width, result.Height, GLPixelFormat.Bgra, GLPixelType.UnsignedByte, result.Data);
                    GL.BindTexture(GLTextureTarget.Texture2D, 0);
                }

                result.Release();
            }

            if (requestedCursor != ImGuiMouseCursor.None)
            {
                ImGui.SetMouseCursor(requestedCursor);
            }

            draw.AddImage(textureId, bb.Min, bb.Max);
        }

        protected override void SetBufferSize(int width, int height)
        {
            if (textureId == 0)
            {
                textureId = GL.GenTexture();
                GL.BindTexture(GLTextureTarget.Texture2D, textureId);

                GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)GLTextureMinFilter.Linear);
                GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)GLTextureMinFilter.Linear);
                GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)GLTextureWrapMode.ClampToEdge);
                GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)GLTextureWrapMode.ClampToEdge);
            }
            else
            {
                GL.BindTexture(GLTextureTarget.Texture2D, textureId);
            }

            GL.TexImage2D(GLTextureTarget.Texture2D, 0, GLInternalFormat.Rgba, width, height, 0, GLPixelFormat.Bgra, GLPixelType.UnsignedByte, 0);

            GL.BindTexture(GLTextureTarget.Texture2D, 0);
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
                GL.DeleteTextures(1, ref textureId);
                textureId = 0;
            }
        }
    }
}