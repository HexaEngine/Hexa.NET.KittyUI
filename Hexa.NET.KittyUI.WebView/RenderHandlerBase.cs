namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Enums;
    using CefSharp.OffScreen;
    using CefSharp.Structs;
    using Hexa.NET.ImGui;
    using Hexa.NET.SDL2;
    using System.Collections.Concurrent;

    public abstract class RenderHandlerBase : IRenderHandler
    {
        protected readonly ConcurrentQueue<CefDrawData> drawQueue = new();
        protected ImGuiMouseCursor requestedCursor;
        private bool disposedValue;
        private int width;
        private int height;

        public bool IsDisposed => disposedValue;

        public int Width => width;

        public int Height => height;

        public void SetSize(int width, int height)
        {
            if (this.width == width && this.height == height) return;
            int oldWidth = width;
            int oldHeight = height;
            this.width = width;
            this.height = height;
            SetBufferSize(oldWidth, oldHeight, width, height);
        }

        protected abstract void SetBufferSize(int oldWidth, int oldHeight, int width, int height);

        public abstract void Draw(ImDrawListPtr draw, ImRect bb, bool hovered);

        public virtual ScreenInfo? GetScreenInfo()
        {
            ScreenInfo info = new()
            {
                DeviceScaleFactor = 1,
                Rect = new(0, 0, width, height),
                AvailableRect = new Rect(0, 0, width, height),
                Depth = 32,
                DepthPerComponent = 8,
                IsMonochrome = false,
            };
            return info;
        }

        public virtual Rect GetViewRect()
        {
            return new Rect(0, 0, width, height);
        }

        public virtual bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        public virtual void OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, AcceleratedPaintInfo acceleratedPaintInfo)
        {
        }

        public virtual void OnCursorChange(nint cursor, CursorType type, CursorInfo customCursorInfo)
        {
            requestedCursor = type switch
            {
                CursorType.Pointer => ImGuiMouseCursor.Arrow,
                CursorType.Cross => ImGuiMouseCursor.Arrow,
                CursorType.Hand => ImGuiMouseCursor.Hand,
                CursorType.IBeam => ImGuiMouseCursor.TextInput,
                CursorType.Wait => ImGuiMouseCursor.NotAllowed, // ImGui doesn't have a "Wait" cursor, closest might be NotAllowed
                CursorType.Help => ImGuiMouseCursor.Arrow, // Custom or unsupported in ImGui by default
                CursorType.EastResize => ImGuiMouseCursor.ResizeEw,
                CursorType.NorthResize => ImGuiMouseCursor.ResizeNs,
                CursorType.NortheastResize => ImGuiMouseCursor.ResizeNesw,
                CursorType.NorthwestResize => ImGuiMouseCursor.ResizeNwse,
                CursorType.SouthResize => ImGuiMouseCursor.ResizeNs,
                CursorType.SoutheastResize => ImGuiMouseCursor.ResizeNwse,
                CursorType.SouthwestResize => ImGuiMouseCursor.ResizeNesw,
                CursorType.WestResize => ImGuiMouseCursor.ResizeEw,
                CursorType.NorthSouthResize => ImGuiMouseCursor.ResizeNs,
                CursorType.EastWestResize => ImGuiMouseCursor.ResizeEw,
                CursorType.NortheastSouthwestResize => ImGuiMouseCursor.ResizeNesw,
                CursorType.NorthwestSoutheastResize => ImGuiMouseCursor.ResizeNwse,
                CursorType.ColumnResize => ImGuiMouseCursor.ResizeEw,
                CursorType.RowResize => ImGuiMouseCursor.ResizeNs,
                CursorType.MiddlePanning => ImGuiMouseCursor.ResizeAll, // Best guess for panning
                CursorType.EastPanning => ImGuiMouseCursor.ResizeEw,
                CursorType.NorthPanning => ImGuiMouseCursor.ResizeNs,
                CursorType.NortheastPanning => ImGuiMouseCursor.ResizeNesw,
                CursorType.NorthwestPanning => ImGuiMouseCursor.ResizeNwse,
                CursorType.SouthPanning => ImGuiMouseCursor.ResizeNs,
                CursorType.SoutheastPanning => ImGuiMouseCursor.ResizeNwse,
                CursorType.SouthwestPanning => ImGuiMouseCursor.ResizeNesw,
                CursorType.WestPanning => ImGuiMouseCursor.ResizeEw,
                CursorType.Move => ImGuiMouseCursor.ResizeAll,
                CursorType.VerticalText => ImGuiMouseCursor.TextInput, // ImGui doesn't have vertical text specifically
                CursorType.Cell => ImGuiMouseCursor.Arrow, // Best match as ImGui doesn't have a dedicated Cell cursor
                CursorType.ContextMenu => ImGuiMouseCursor.Arrow, // ImGui doesn't support this directly
                CursorType.Alias => ImGuiMouseCursor.Arrow, // No alias cursor in ImGui
                CursorType.Progress => ImGuiMouseCursor.Arrow, // No dedicated progress in ImGui
                CursorType.NoDrop => ImGuiMouseCursor.NotAllowed,
                CursorType.Copy => ImGuiMouseCursor.Arrow, // No direct copy cursor in ImGui
                CursorType.None => ImGuiMouseCursor.None,
                CursorType.NotAllowed => ImGuiMouseCursor.NotAllowed,
                CursorType.ZoomIn => ImGuiMouseCursor.Arrow, // No zoom in/out in ImGui
                CursorType.ZoomOut => ImGuiMouseCursor.Arrow, // No zoom in/out in ImGui
                CursorType.Grab => ImGuiMouseCursor.Hand, // Hand is closest to grab in ImGui
                CursorType.Grabbing => ImGuiMouseCursor.Hand, // Same as grab in ImGui
                CursorType.MiddlePanningVertical => ImGuiMouseCursor.ResizeNs,
                CursorType.MiddlePanningHorizontal => ImGuiMouseCursor.ResizeEw,
                CursorType.Custom => ImGuiMouseCursor.Arrow, // ImGui doesn't support custom cursors natively
                CursorType.DndNone => ImGuiMouseCursor.None,
                CursorType.DndMove => ImGuiMouseCursor.ResizeAll,
                CursorType.DndCopy => ImGuiMouseCursor.Arrow, // ImGui doesn't differentiate DnD cursor types
                CursorType.DndLink => ImGuiMouseCursor.Arrow, // Same limitation as above
                _ => ImGuiMouseCursor.Arrow // Default fallback
            };
        }

        public virtual unsafe void OnPaint(PaintElementType type, Rect dirtyRect, nint buffer, int width, int height)
        {
            int sizeInBytes = width * 4 * height;
            byte* buf = AllocT<byte>(sizeInBytes);
            Memcpy((void*)buffer, buf, sizeInBytes);

            CefDrawData data = new(type, dirtyRect, width, height, buf);

            drawQueue.Enqueue(data);
        }

        public abstract void OnPopupShow(bool show);

        public abstract void OnPopupSize(Rect rect);

        public virtual void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            if (SDL.HasScreenKeyboardSupport() == SDLBool.True)
            {
                if (inputMode == TextInputMode.None)
                {
                    SDL.SetHint(SDL.SDL_HINT_ENABLE_SCREEN_KEYBOARD, "0");
                    SDL.StopTextInput();
                }
                else
                {
                    SDL.SetHint(SDL.SDL_HINT_ENABLE_SCREEN_KEYBOARD, "1");
                    SDL.StartTextInput();
                }
            }
        }

        public virtual void OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
        }

        public abstract bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y);

        public abstract void UpdateDragCursor(DragOperationsMask operation);

        protected abstract void DisposeCore();

        public void Dispose()
        {
            if (!disposedValue)
            {
                DisposeCore();
                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}