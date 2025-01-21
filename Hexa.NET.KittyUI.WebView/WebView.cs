namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Handler;
    using CefSharp.Internals;
    using CefSharp.OffScreen;
    using CefSharp.Web;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public partial class WebView : IRenderWebBrowser, IDisposable
    {
        private readonly ChromiumWebBrowser browser;
        private readonly RenderHandlerBase renderHandler;
        private IBrowserHost? host;
        private Point2 size;
        private Point2 position;
        private CefEventFlags eventFlags;

        private float wheelDeltaAccumulatorX = 0f;
        private float wheelDeltaAccumulatorY = 0f;
        private Point2 scrollScale = new(120);

        private bool smoothScroll = true;
        private Point2 scrollPosition;
        private float scrollSpeed = 120f * 40;

        private bool isHovered;
        private bool hasFocus;
        private uint windowId;

        private bool disposedValue;

        public WebView() : this(address: null)
        {
        }

        public WebView(string? address = null, BrowserSettings? settings = null, IRequestContext? requestContext = null, RenderHandlerBase? renderHandler = null)
        {
            CefManager.Initialize();

            this.renderHandler = renderHandler ??= CefManager.CreateRenderer();

            var mode = Display.GetDesktopDisplayMode(0);

            settings ??= CefManager.GetDefaultBrowserSettings();

            browser = new(address, settings, requestContext)
            {
                RenderHandler = renderHandler,
                LifeSpanHandler = new LifeSpanHandler(),
                LoadHandler = new LoadHandler(),
                DisplayHandler = new DisplayHandler(),
                AudioHandler = new AudioHandler(),
            };

            browser.BrowserInitialized += OnBrowserInitialized;

            Application.RegisterHook(MessageHook);
        }

        public WebView(HtmlString htmlString, BrowserSettings? settings = null, IRequestContext? requestContext = null, RenderHandlerBase? renderHandler = null)
        {
            CefManager.Initialize();

            this.renderHandler = renderHandler ??= CefManager.CreateRenderer();

            var mode = Display.GetDesktopDisplayMode(0);

            settings ??= CefManager.GetDefaultBrowserSettings();

            browser = new(htmlString, settings, requestContext)
            {
                RenderHandler = renderHandler,
                LifeSpanHandler = new LifeSpanHandler(),
                LoadHandler = new LoadHandler(),
                DisplayHandler = new DisplayHandler(),
                AudioHandler = new AudioHandler(),
            };

            OnCreateBrowser(browser);

            browser.BrowserInitialized += OnBrowserInitialized;

            Application.RegisterHook(MessageHook);
        }

        public ChromiumWebBrowser Browser => browser;

        public float ScrollSpeed { get => scrollSpeed; set => scrollSpeed = value; }

        public bool SmoothScroll { get => smoothScroll; set => smoothScroll = value; }

        public Point2 ScrollScale { get => scrollScale; set => scrollScale = value; }

        public Point2 Size
        {
            get => size;
            set
            {

                size = value;
                renderHandler.SetSize(value.X, value.Y);

                host?.WasResized();

                host?.Invalidate(PaintElementType.View);
            }
        }

        protected virtual void OnBrowserInitialized(object? sender, EventArgs e)
        {
            browser.BrowserInitialized -= OnBrowserInitialized;
            host = browser.GetBrowserHost();
        }

        protected virtual void OnCreateBrowser(ChromiumWebBrowser browser)
        {
        }

        protected virtual void OnFocusLost()
        {
            hasFocus = false;
            host?.SendFocusEvent(false);
        }

        protected virtual void OnFocusGained()
        {
            hasFocus = true;
            host?.SendFocusEvent(true);
            ImGui.SetKeyboardFocusHere();
        }

        protected virtual void OnLeave()
        {
            isHovered = false;
            var pos = GetMousePos();
            host?.SendMouseMoveEvent(new(pos.X, pos.Y, eventFlags), true);
        }

        protected virtual void OnEnter()
        {
            isHovered = true;
        }

        private bool MessageHook(SDLEvent evnt)
        {
            if (host == null)
            {
                return false;
            }

            if (!isHovered) return false;

            switch ((SDLEventType)evnt.Type)
            {
                case SDLEventType.Mousemotion:
                    if (evnt.Motion.WindowID != windowId)
                        return false;
                    HandleMouseMove(evnt.Motion);
                    return true;

                case SDLEventType.Mousewheel:
                    if (evnt.Wheel.WindowID != windowId)
                        return false;
                    HandleMouseWheel(evnt.Wheel);
                    return true;

                case SDLEventType.Mousebuttondown:
                case SDLEventType.Mousebuttonup:
                    if (evnt.Button.WindowID != windowId)
                        return false;
                    HandleMouseButton(evnt.Button);
                    return true;

                case SDLEventType.Keydown:
                case SDLEventType.Keyup:
                    if (evnt.Key.WindowID != windowId)
                        return false;
                    HandleKeyboard(evnt.Key);
                    return true;

                case SDLEventType.Textinput:
                    if (evnt.Text.WindowID != windowId)
                        return false;
                    HandleTextInput(evnt.Text);
                    return true;

                case SDLEventType.Dropbegin:
                case SDLEventType.Dropcomplete:
                case SDLEventType.Droptext:
                case SDLEventType.Dropfile:
                    if (evnt.Drop.WindowID != windowId)
                        return false;
                    HandleDragDrop(evnt.Drop);
                    return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDragDrop(SDLDropEvent drop)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void HandleTextInput(SDLTextInputEvent text)
        {
            byte* textPtr = &text.Text_0;
            int i = 0;

            while (textPtr[i] != 0)
            {
                byte currentByte = textPtr[i];

                if (currentByte < 0x80)
                {
                    int character = currentByte;
                    SendCharEvent(character);
                    i++;
                }
                else if ((currentByte & 0xE0) == 0xC0) // 2-byte character (110xxxxx)
                {
                    byte nextByte = textPtr[i + 1];
                    if ((nextByte & 0xC0) == 0x80) // 10xxxxxx
                    {
                        int character = ((currentByte & 0x1F) << 6) | (nextByte & 0x3F);
                        SendCharEvent(character);
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if ((currentByte & 0xF0) == 0xE0) // 3-byte character (1110xxxx)
                {
                    // Get the next two bytes and combine them into a character
                    byte nextByte1 = textPtr[i + 1];
                    byte nextByte2 = textPtr[i + 2];
                    if ((nextByte1 & 0xC0) == 0x80 && (nextByte2 & 0xC0) == 0x80)
                    {
                        int character = ((currentByte & 0x0F) << 12) | ((nextByte1 & 0x3F) << 6) | (nextByte2 & 0x3F);
                        SendCharEvent(character);
                        i += 3;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if ((currentByte & 0xF8) == 0xF0) // 4-byte character (11110xxx)
                {
                    byte nextByte1 = textPtr[i + 1];
                    byte nextByte2 = textPtr[i + 2];
                    byte nextByte3 = textPtr[i + 3];
                    if ((nextByte1 & 0xC0) == 0x80 && (nextByte2 & 0xC0) == 0x80 && (nextByte3 & 0xC0) == 0x80)
                    {
                        int codepoint = ((currentByte & 0x07) << 18) | ((nextByte1 & 0x3F) << 12) | ((nextByte2 & 0x3F) << 6) | (nextByte3 & 0x3F);
                        SendCharEvent(codepoint);
                        i += 4;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendCharEvent(int utf32Code)
        {
            KeyEvent charEvent = new()
            {
                Type = KeyEventType.Char,
                WindowsKeyCode = utf32Code,
                NativeKeyCode = utf32Code,
                IsSystemKey = false,
                Modifiers = eventFlags
            };

            host!.SendKeyEvent(charEvent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleKeyboard(SDLKeyboardEvent key)
        {
            GetCefModifiers((SDLKeymod)key.Keysym.Mod);
            var flags = eventFlags;

            if (key.Repeat == 1)
            {
                flags |= CefEventFlags.IsRepeat;
            }

            KeyEvent keyEvent = new()
            {
                Type = key.Type == (int)SDLEventType.Keydown ? KeyEventType.KeyDown : KeyEventType.KeyUp,
                NativeKeyCode = (int)key.Keysym.Scancode,
                WindowsKeyCode = (int)CefHelper.MapSDLKeyCodeToVirtualKey((SDLKeyCode)key.Keysym.Sym),
                IsSystemKey = false,
                Modifiers = flags,
            };

            host!.SendKeyEvent(keyEvent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetCefModifiers(SDLKeymod mod)
        {
            if ((mod & SDLKeymod.Shift) != 0)
            {
                eventFlags |= CefEventFlags.ShiftDown;
            }
            else
            {
                eventFlags &= ~CefEventFlags.ShiftDown;
            }

            if ((mod & SDLKeymod.Ctrl) != 0)
            {
                eventFlags |= CefEventFlags.ControlDown;
            }
            else
            {
                eventFlags &= ~CefEventFlags.ControlDown;
            }

            if ((mod & SDLKeymod.Alt) != 0)
            {
                eventFlags |= CefEventFlags.AltDown;
            }
            else
            {
                eventFlags &= ~CefEventFlags.AltDown;
            }

            if ((mod & SDLKeymod.Gui) != 0)
            {
                eventFlags |= CefEventFlags.CommandDown;
            }
            else
            {
                eventFlags &= ~CefEventFlags.CommandDown;
            }

            if ((mod & SDLKeymod.Caps) != 0)
            {
                eventFlags |= CefEventFlags.CapsLockOn;
            }
            else
            {
                eventFlags &= ~CefEventFlags.CapsLockOn;
            }

            if ((mod & SDLKeymod.Num) != 0)
            {
                eventFlags |= CefEventFlags.NumLockOn;
            }
            else
            {
                eventFlags &= ~CefEventFlags.NumLockOn;
            }

            if ((mod & SDLKeymod.Mode) != 0)
            {
                eventFlags |= CefEventFlags.AltGrDown;
            }
            else
            {
                eventFlags &= ~CefEventFlags.AltGrDown;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleMouseButton(SDLMouseButtonEvent button)
        {
            var pos = GetMousePos();
            MouseEvent mouseEvent = new(pos.X, pos.Y, CefEventFlags.None);
            MouseButtonType type = (MouseButton)button.Button switch
            {
                MouseButton.Left => MouseButtonType.Left,
                MouseButton.Right => MouseButtonType.Right,
                MouseButton.Middle => MouseButtonType.Middle,
                _ => 0
            };

            bool isUp = button.Type == (int)SDLEventType.Mousebuttonup;
            if (isUp)
            {
                if (type == MouseButtonType.Left)
                {
                    eventFlags &= ~CefEventFlags.LeftMouseButton;
                }

                if (type == MouseButtonType.Right)
                {
                    eventFlags &= ~CefEventFlags.RightMouseButton;
                }

                if (type == MouseButtonType.Middle)
                {
                    eventFlags &= ~CefEventFlags.MiddleMouseButton;
                }
            }
            else
            {
                if (type == MouseButtonType.Left)
                {
                    eventFlags |= CefEventFlags.LeftMouseButton;
                }

                if (type == MouseButtonType.Right)
                {
                    eventFlags |= CefEventFlags.RightMouseButton;
                }

                if (type == MouseButtonType.Middle)
                {
                    eventFlags |= CefEventFlags.MiddleMouseButton;
                }
            }

            host!.SendMouseClickEvent(mouseEvent, type, isUp, button.Clicks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleMouseMove(SDLMouseMotionEvent motion)
        {
            var pos = GetMousePos();
            MouseEvent mouseEvent = new(pos.X, pos.Y, eventFlags);
            host!.SendMouseMoveEvent(mouseEvent, mouseLeave: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Point2 GetMousePos()
        {
            int x, y;
            SDL.GetGlobalMouseState(&x, &y);
            Vector2 point = new(x - position.X, y - position.Y);
            return point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleMouseWheel(SDLMouseWheelEvent wheel)
        {
            var pos = GetMousePos();
            if (smoothScroll)
            {
                wheelDeltaAccumulatorX += wheel.X * scrollScale.X;
                wheelDeltaAccumulatorY += wheel.Y * scrollScale.Y;
                scrollPosition = new(pos.X, pos.Y);
            }
            else
            {
                MouseEvent mouseEvent = new(pos.X, pos.Y, eventFlags);
                host!.SendMouseWheelEvent(mouseEvent, wheel.X * scrollScale.X, wheel.Y * scrollScale.Y);
            }
        }

        protected virtual void UpdateScroll(ref float wheelDeltaAccumulator)
        {
            if (host != null && Math.Abs(wheelDeltaAccumulator) > 0)
            {
                float maxScrollDelta = scrollSpeed * ImGui.GetIO().DeltaTime;
                float scrollDelta = Math.Sign(wheelDeltaAccumulator) * Math.Min(Math.Abs(wheelDeltaAccumulator), Math.Max(maxScrollDelta, 1));
                int intScrollDelta = (int)scrollDelta;
                MouseEvent mouseEvent = new(scrollPosition.X, scrollPosition.Y, eventFlags);
                host.SendMouseWheelEvent(mouseEvent, 0, intScrollDelta);

                wheelDeltaAccumulator -= intScrollDelta;
            }
        }

        public unsafe void Draw(string strId)
        {
            byte* pStr0 = null;
            int strSize0 = 0;
            if (strId != null)
            {
                strSize0 = HexaGen.Runtime.Utils.GetByteCountUTF8(strId);
                if (strSize0 > HexaGen.Runtime.Utils.MaxStackallocSize)
                {
                    pStr0 = HexaGen.Runtime.Utils.Alloc<byte>(strSize0 + 1);
                }
                else
                {
                    byte* pStrStack0 = stackalloc byte[strSize0 + 1];
                    pStr0 = pStrStack0;
                }
                HexaGen.Runtime.Utils.EncodeStringUTF8(strId, pStr0, strSize0);
            }

            Draw(pStr0);

            if (strSize0 > HexaGen.Runtime.Utils.MaxStackallocSize)
            {
                HexaGen.Runtime.Utils.Free(pStr0);
            }
        }

        public unsafe void Draw(ReadOnlySpan<byte> strId)
        {
            fixed (byte* pStrId = strId)
            {
                Draw(pStrId);
            }
        }

        public unsafe void Draw(byte* strId)
        {
            var window = ImGuiP.GetCurrentWindow();
            if (window.SkipItems)
            {
                return;
            }

            uint id = ImGui.GetID(strId);

            UpdateScroll(ref wheelDeltaAccumulatorX);
            UpdateScroll(ref wheelDeltaAccumulatorY);

            Point2 newPos = ImGui.GetCursorScreenPos();

            if (newPos != position)
            {
                host?.NotifyMoveOrResizeStarted();
            }

            position = newPos;
            ImRect bb = new(position, position + size);
            ImGuiP.ItemSize(bb);

            if (!ImGuiP.ItemAdd(bb, id, ref bb, 0))
            {
                return;
            }

            bool hovered = ImGuiP.ItemHoverable(bb, id, ImGuiItemFlags.None);

            bool focused = ImGui.IsItemFocused();

            bool clicked = hovered && ImGuiP.IsMouseClicked(ImGuiMouseButton.Left);

            var viewport = ImGui.GetWindowViewport();

            windowId = (uint)viewport.PlatformHandle;

            if (isHovered != hovered)
            {
                if (hovered)
                {
                    OnEnter();
                }
                else
                {
                    OnLeave();
                }
            }

            if (clicked && !hasFocus)
            {
                OnFocusGained();
            }

            if (!focused && hasFocus)
            {
                OnFocusLost();
            }

            if (hovered)
            {
                ImGuiP.SetItemKeyOwner(ImGuiKey.MouseWheelY);
                ImGuiP.SetItemKeyOwner(ImGuiKey.MouseWheelX);
                ImGuiP.SetItemKeyOwner(ImGuiKey.Tab);
                ImGuiP.SetItemKeyOwner(ImGuiKey.UpArrow);
                ImGuiP.SetItemKeyOwner(ImGuiKey.DownArrow);
                ImGuiP.SetItemKeyOwner(ImGuiKey.LeftArrow);
                ImGuiP.SetItemKeyOwner(ImGuiKey.RightArrow);
            }

            var draw = ImGui.GetWindowDrawList();

          

            renderHandler.Draw(draw, bb, hovered);
        }

        protected virtual void DisposeCore()
        {
            Application.UnregisterHook(MessageHook);
            browser.Dispose();
        }

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