namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Native.Windows;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Logging;
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using static Hexa.NET.KittyUI.UI.Taskbar;

    public unsafe class TitleBar
    {
        public event EventHandler<CloseWindowRequest>? CloseWindowRequest;

        public event EventHandler<MaximizeWindowRequest>? MaximizeWindowRequest;

        public event EventHandler<MinimizeWindowRequest>? MinimizeWindowRequest;

        public event EventHandler<RestoreWindowRequest>? RestoreWindowRequest;

        public CoreWindow Window { get; set; } = null!;

        private bool isDragging = false;
        private Point2 dragOffset;
        private ImDrawListPtr draw;
        private Vector2 titleBarPos;
        private Vector2 titleBarSize;
        private int titleBarHeight = 30;
        private Point2 mousePos;
        private Vector2 cursorPos;
        const float buttonSize = 50;

        public int Height { get => titleBarHeight; set => titleBarHeight = value; }

        public virtual void Draw()
        {
            var viewport = ImGui.GetMainViewport();
            draw = ImGui.GetForegroundDrawList(viewport);

            // Draw the custom title bar
            titleBarPos = viewport.Pos; // Start at the top of the viewport
            titleBarSize = new Vector2(viewport.Size.X, titleBarHeight); // Full width of the viewport
            mousePos = Mouse.Global;
            cursorPos = titleBarPos;

            ImRect rect = new(titleBarPos, titleBarPos + titleBarSize);

            uint color = Window.Focused ? ImGui.GetColorU32(ImGuiCol.TitleBgActive) : ImGui.GetColorU32(ImGuiCol.TitleBg);
            // Draw a filled rectangle for the title bar background
            draw.AddRectFilled(rect.Min, rect.Max, color);

            // Draw the title text centered in the title bar
            string title = Window.Title;
            var textSize = ImGui.CalcTextSize(title);
            var textPos = new Vector2(
                titleBarPos.X + (titleBarSize.X - textSize.X) * 0.5f,
                titleBarPos.Y + (titleBarSize.Y - textSize.Y) * 0.5f
            );
            draw.AddText(textPos, 0xFFFFFFFF, title);

            bool handled = false;

            cursorPos.X = rect.Max.X - buttonSize * 3;

            if (Button($"{MaterialIcons.Remove}", 0x1CCCCCCC, 0x1CCCCCCC, new(buttonSize, titleBarHeight), ref handled)) // 0xCCCCCCCC is a color ABGR
            {
                RequestMinimize();
            }

            if (Button($"{MaterialIcons.SelectWindow2}", 0x1CCCCCCC, 0x1CCCCCCC, new(buttonSize, titleBarHeight), ref handled))
            {
                if (Window.State == WindowState.Maximized)
                {
                    RequestRestore();
                }
                else
                {
                    RequestMaximize();
                }
            }

            if (Button($"{MaterialIcons.Close}", 0xFF3333C6, 0xFF3333C6, new(buttonSize, titleBarHeight), ref handled))
            {
                RequestClose();
            }

            // Adjust the cursor position to avoid drawing ImGui elements under the custom title bar
            viewport.WorkPos.Y += titleBarHeight;
            viewport.WorkSize.Y -= titleBarHeight;
        }

        private unsafe bool Button(string label, uint hoveredColor, uint activeColor, Vector2 size, ref bool handled)
        {
            int byteCount = Encoding.UTF8.GetByteCount(label);
            byte* pLabel;
            if (byteCount > StackAllocLimit)
            {
                pLabel = (byte*)Alloc(byteCount + 1);
            }
            else
            {
                byte* stackLabel = stackalloc byte[byteCount + 1];
                pLabel = stackLabel;
            }
            int offset = Encoding.UTF8.GetBytes(label, new Span<byte>(pLabel, byteCount));
            pLabel[offset] = 0;

            bool result = Button(pLabel, hoveredColor, activeColor, size, ref handled);

            if (byteCount > StackAllocLimit)
            {
                Free(pLabel);
            }

            return result;
        }

        private unsafe bool Button(ReadOnlySpan<byte> label, uint hoveredColor, uint activeColor, Vector2 size, ref bool handled)
        {
            fixed (byte* pLabel = label)
            {
                return Button(pLabel, hoveredColor, activeColor, size, ref handled);
            }
        }

        private unsafe bool Button(byte* label, uint hoveredColor, uint activeColor, Vector2 size, ref bool handled)
        {
            var mousePos = Mouse.Global;
            // Draw a custom close button on the right side of the title bar
            var pos = cursorPos;

            cursorPos += new Vector2(size.X, 0);

            ImRect rect = new(pos, pos + size);

            bool isHovered = rect.Contains(mousePos);
            bool isMouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left) && isHovered;

            uint color = isMouseDown ? activeColor : isHovered ? hoveredColor : 0;

            if (isHovered || isMouseDown)
            {
                draw.AddRectFilled(rect.Min, rect.Max, color);
            }

            bool clicked = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
            var textSizeClose = ImGui.CalcTextSize(label);
            var midpoint = rect.Midpoint() - textSizeClose / 2;
            draw.AddText(midpoint, 0xFFFFFFFF, label);
            handled |= isHovered;

            if (isHovered && clicked)
            {
                return true;
            }

            return false;
        }

        public void RequestClose()
        {
            CloseWindowRequest request = new(Window);
            OnWindowCloseRequest(request);
            if (!request.Handled)
            {
                CloseWindowRequest?.Invoke(this, request);
            }
        }

        public unsafe void RequestMinimize()
        {
            MinimizeWindowRequest request = new(Window);
            OnWindowMinimizeRequest(request);
            if (!request.Handled)
            {
                MinimizeWindowRequest?.Invoke(this, request);
            }
        }

        public void RequestMaximize()
        {
            MaximizeWindowRequest request = new(Window);
            OnWindowMaximizeRequest(request);
            if (!request.Handled)
            {
                MaximizeWindowRequest?.Invoke(this, request);
            }
        }

        public void RequestRestore()
        {
            RestoreWindowRequest request = new(Window);
            OnWindowRestoreRequest(request);
            if (!request.Handled)
            {
                RestoreWindowRequest?.Invoke(this, request);
            }
        }

        protected virtual void OnWindowCloseRequest(CloseWindowRequest args)
        {
        }

        protected virtual void OnWindowMinimizeRequest(MinimizeWindowRequest args)
        {
        }

        protected virtual void OnWindowMaximizeRequest(MaximizeWindowRequest args)
        {
        }

        protected virtual void OnWindowRestoreRequest(RestoreWindowRequest args)
        {
        }

        public virtual SDLHitTestResult HitTest(SDLWindow* win, SDLPoint* area, void* data)
        {
            int w, h;
            SDL.SDLGetWindowSize(win, &w, &h);
            if (area->X < w - buttonSize * 3)
            {
                return SDLHitTestResult.Draggable;
            }

            return SDLHitTestResult.Normal;
        }

        public virtual void OnAttach(CoreWindow window)
        {
            Window = window;
            if (OperatingSystem.IsWindows())
            {
                InjectInterceptor(window.GetHWND());
            }
        }

        public virtual void OnDetach(CoreWindow window)
        {
            if (OperatingSystem.IsWindows())
            {
                RemoveInterceptor(window.GetHWND());
            }
            Window = null!;
        }

        #region WIN32

        private void* originalWndProc;

        [SupportedOSPlatform("windows")]
        private void InjectInterceptor(nint hwnd)
        {
            void* injector = (void*)Marshal.GetFunctionPointerForDelegate<WndProc>(MyWndProc);
            originalWndProc = WinApi.SetWindowLongPtr(hwnd, WinApi.GWLP_WNDPROC, injector);
            WinApi.SetWindowPos(hwnd, 0, 0, 0, 0, 0, WinApi.SWP_FRAMECHANGED | WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOZORDER | WinApi.SWP_NOACTIVATE);
        }

        [SupportedOSPlatform("windows")]
        private void RemoveInterceptor(nint hwnd)
        {
            WinApi.SetWindowLongPtr(hwnd, WinApi.GWLP_WNDPROC, originalWndProc);
        }

        [SupportedOSPlatform("windows")]
        private nint MyWndProc(nint hwnd, uint message, nint wParam, nint lParam)
        {
            if (message == WinApi.WM_NCCALCSIZE && wParam != 0)
            {
                // Cast the pointer to NCCALCSIZE_PARAMS
                NcCalcSizeParams* nccsp = (NcCalcSizeParams*)lParam;

                WindowPos* winPos = nccsp->LpPos;

                if (WinApi.IsZoomed(hwnd))
                {
                    nccsp->RgRc0.Top = Math.Max(nccsp->RgRc0.Top - titleBarHeight, -titleBarHeight - 1);
                }
                else
                {
                    nccsp->RgRc0.Top = nccsp->RgRc0.Top - titleBarHeight;
                }
            }

            return WinApi.CallWindowProc(originalWndProc, hwnd, message, wParam, lParam);
        }

        #endregion WIN32
    }
}