namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.DevTools.IndexedDB;
    using CefSharp.Handler;
    using CefSharp.OffScreen;
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;
    using System;
    using System.Numerics;
    using System.Threading;

    public class WebView
    {
        private readonly ChromiumWebBrowser browser;
        private readonly RenderHandlerBase handlerBase;
        private IBrowserHost? host;
        private Point2 size;
        private Point2 position;
        private CefEventFlags eventFlags;

        private float wheelDeltaAccumulator = 0f;

        private Point2 scrollPosition;
        private const float ScrollSpeed = 120f * 40;

        private bool hoveredBefore;
        private bool hasFocus;

        public WebView()
        {
            CefManager.Initialize();

            switch (Application.GraphicsBackend)
            {
                case GraphicsBackend.D3D11:
                    handlerBase = new D3D11RenderHandler();
                    break;

                case GraphicsBackend.OpenGL:
                    handlerBase = new OpenGLRenderHandler();
                    break;

                default:
                    throw new PlatformNotSupportedException();
            }

            var mode = Display.GetDesktopDisplayMode(0);
            browser = new(browserSettings: new BrowserSettings
            {
                WindowlessFrameRate = mode.RefreshRate,
                Javascript = CefState.Enabled,
                WebGl = CefState.Enabled,
            })
            {
                RenderHandler = handlerBase,
                LifeSpanHandler = new LifeSpanHandler(),
                LoadHandler = new LoadHandler(),
                DisplayHandler = new DisplayHandler(),
                AudioHandler = new AudioHandler(),
            };

            browser.BrowserInitialized += Browser_BrowserInitialized;

            Application.RegisterHook(Hook);
        }

        public ChromiumWebBrowser Browser => browser;

        public Point2 Size
        {
            get => size;
            set
            {
                size = value;
                handlerBase.SetSize(value.X, value.Y);
                host?.WasResized();
            }
        }

        private void Browser_BrowserInitialized(object? sender, EventArgs e)
        {
            browser.BrowserInitialized -= Browser_BrowserInitialized;
            host = browser.GetBrowserHost();
        }

        private bool Hook(SDLEvent evnt)
        {
            if (host == null)
            {
                return false;
            }

            switch ((SDLEventType)evnt.Type)
            {
                case SDLEventType.Mousemotion:
                    HandleMouseMove(evnt.Motion);
                    break;

                case SDLEventType.Mousewheel:
                    HandleMouseWheel(evnt.Wheel);
                    break;

                case SDLEventType.Mousebuttondown:
                case SDLEventType.Mousebuttonup:
                    HandleMouseButton(evnt.Button);
                    break;

                case SDLEventType.Keydown:
                case SDLEventType.Keyup:
                    HandleKeyboard(evnt.Key);
                    break;

                case SDLEventType.Textinput:
                    HandleTextInput(evnt.Text);
                    break;
            }
            return false;
        }

        public static VirtualKey MapSDLKeyCodeToVirtualKey(SDLKeyCode sdlKeyCode) => sdlKeyCode switch
        {
            SDLKeyCode.Unknown => throw new ArgumentOutOfRangeException(nameof(sdlKeyCode), "Unknown SDL key code"),
            SDLKeyCode.Return => VirtualKey.Return,
            SDLKeyCode.Escape => VirtualKey.Escape,
            SDLKeyCode.Backspace => VirtualKey.Back,
            SDLKeyCode.Tab => VirtualKey.Tab,
            SDLKeyCode.Space => VirtualKey.Space,
            SDLKeyCode.A => VirtualKey.A,
            SDLKeyCode.B => VirtualKey.B,
            SDLKeyCode.C => VirtualKey.C,
            SDLKeyCode.D => VirtualKey.D,
            SDLKeyCode.E => VirtualKey.E,
            SDLKeyCode.F => VirtualKey.F,
            SDLKeyCode.G => VirtualKey.G,
            SDLKeyCode.H => VirtualKey.H,
            SDLKeyCode.I => VirtualKey.I,
            SDLKeyCode.J => VirtualKey.J,
            SDLKeyCode.K => VirtualKey.K,
            SDLKeyCode.L => VirtualKey.L,
            SDLKeyCode.M => VirtualKey.M,
            SDLKeyCode.N => VirtualKey.N,
            SDLKeyCode.O => VirtualKey.O,
            SDLKeyCode.P => VirtualKey.P,
            SDLKeyCode.Q => VirtualKey.Q,
            SDLKeyCode.R => VirtualKey.R,
            SDLKeyCode.S => VirtualKey.S,
            SDLKeyCode.T => VirtualKey.T,
            SDLKeyCode.U => VirtualKey.U,
            SDLKeyCode.V => VirtualKey.V,
            SDLKeyCode.W => VirtualKey.W,
            SDLKeyCode.X => VirtualKey.X,
            SDLKeyCode.Y => VirtualKey.Y,
            SDLKeyCode.Z => VirtualKey.Z,
            SDLKeyCode.F1 => VirtualKey.F1,
            SDLKeyCode.F2 => VirtualKey.F2,
            SDLKeyCode.F3 => VirtualKey.F3,
            SDLKeyCode.F4 => VirtualKey.F4,
            SDLKeyCode.F5 => VirtualKey.F5,
            SDLKeyCode.F6 => VirtualKey.F6,
            SDLKeyCode.F7 => VirtualKey.F7,
            SDLKeyCode.F8 => VirtualKey.F8,
            SDLKeyCode.F9 => VirtualKey.F9,
            SDLKeyCode.F10 => VirtualKey.F10,
            SDLKeyCode.F11 => VirtualKey.F11,
            SDLKeyCode.F12 => VirtualKey.F12,
            SDLKeyCode.Insert => VirtualKey.Insert,
            SDLKeyCode.Delete => VirtualKey.Delete,
            SDLKeyCode.Home => VirtualKey.Home,
            SDLKeyCode.End => VirtualKey.End,
            SDLKeyCode.Pageup => VirtualKey.Prior,
            SDLKeyCode.Pagedown => VirtualKey.Next,
            SDLKeyCode.Left => VirtualKey.Left,
            SDLKeyCode.Right => VirtualKey.Right,
            SDLKeyCode.Up => VirtualKey.Up,
            SDLKeyCode.Down => VirtualKey.Down,
            SDLKeyCode.Capslock => VirtualKey.Capital,
            SDLKeyCode.Numlockclear => VirtualKey.NumLock,
            SDLKeyCode.Scrolllock => VirtualKey.Scroll,
            SDLKeyCode.Lctrl => VirtualKey.LControl,
            SDLKeyCode.Rctrl => VirtualKey.RControl,
            SDLKeyCode.Lshift => VirtualKey.LShift,
            SDLKeyCode.Rshift => VirtualKey.RShift,
            SDLKeyCode.Lalt => VirtualKey.LMenu,
            SDLKeyCode.Ralt => VirtualKey.RMenu,
            SDLKeyCode.Lgui => VirtualKey.LWin,
            SDLKeyCode.Rgui => VirtualKey.RWin,
            SDLKeyCode.Menu => VirtualKey.Apps,
            SDLKeyCode.K0 => VirtualKey.D0,
            SDLKeyCode.K1 => VirtualKey.D1,
            SDLKeyCode.K2 => VirtualKey.D2,
            SDLKeyCode.K3 => VirtualKey.D3,
            SDLKeyCode.K4 => VirtualKey.D4,
            SDLKeyCode.K5 => VirtualKey.D5,
            SDLKeyCode.K6 => VirtualKey.D6,
            SDLKeyCode.K7 => VirtualKey.D7,
            SDLKeyCode.K8 => VirtualKey.D8,
            SDLKeyCode.K9 => VirtualKey.D9,
            SDLKeyCode.KpDivide => VirtualKey.Divide,
            SDLKeyCode.KpMultiply => VirtualKey.Multiply,
            SDLKeyCode.KpMinus => VirtualKey.Subtract,
            SDLKeyCode.KpPlus => VirtualKey.Add,
            SDLKeyCode.KpEnter => VirtualKey.Return,
            SDLKeyCode.Kp0 => VirtualKey.Numpad0,
            SDLKeyCode.Kp1 => VirtualKey.Numpad1,
            SDLKeyCode.Kp2 => VirtualKey.Numpad2,
            SDLKeyCode.Kp3 => VirtualKey.Numpad3,
            SDLKeyCode.Kp4 => VirtualKey.Numpad4,
            SDLKeyCode.Kp5 => VirtualKey.Numpad5,
            SDLKeyCode.Kp6 => VirtualKey.Numpad6,
            SDLKeyCode.Kp7 => VirtualKey.Numpad7,
            SDLKeyCode.Kp8 => VirtualKey.Numpad8,
            SDLKeyCode.Kp9 => VirtualKey.Numpad9,
            SDLKeyCode.KpPeriod => VirtualKey.Decimal,
            SDLKeyCode.Printscreen => VirtualKey.Snapshot,
            SDLKeyCode.Pause => VirtualKey.Pause,
            SDLKeyCode.Help => VirtualKey.Help,
            SDLKeyCode.Audionext => VirtualKey.MediaNextTrack,
            SDLKeyCode.Audioprev => VirtualKey.MediaPrevTrack,
            SDLKeyCode.Audiostop => VirtualKey.MediaStop,
            SDLKeyCode.Audioplay => VirtualKey.MediaPlayPause,
            SDLKeyCode.Audiomute => VirtualKey.VolumeMute,
            SDLKeyCode.Volumedown => VirtualKey.VolumeDown,
            SDLKeyCode.Volumeup => VirtualKey.VolumeUp,
            SDLKeyCode.Sleep => VirtualKey.Sleep,
            SDLKeyCode.Again => VirtualKey.Execute,
            SDLKeyCode.Clear => VirtualKey.Clear,
            SDLKeyCode.Crsel => VirtualKey.CrSel,
            SDLKeyCode.Exsel => VirtualKey.ExSel,
            SDLKeyCode.Mode => VirtualKey.ModeChange,
            SDLKeyCode.KpEquals => VirtualKey.OemPlus,
            SDLKeyCode.KpComma => VirtualKey.OemComma,

            _ => throw new ArgumentOutOfRangeException(nameof(sdlKeyCode), $"Unhandled SDLKeyCode: {sdlKeyCode}")
        };

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
                WindowsKeyCode = (int)MapSDLKeyCodeToVirtualKey((SDLKeyCode)key.Keysym.Sym),
                IsSystemKey = false,
                Modifiers = flags,
            };

            host!.SendKeyEvent(keyEvent);
        }

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

        private void HandleMouseMove(SDLMouseMotionEvent motion)
        {
            var pos = GetMousePos();
            MouseEvent mouseEvent = new(pos.X, pos.Y, eventFlags);
            host!.SendMouseMoveEvent(mouseEvent, mouseLeave: false);
        }

        private unsafe Point2 GetMousePos()
        {
            int x, y;
            SDL.GetGlobalMouseState(&x, &y);
            Vector2 point = new(x - position.X, y - position.Y);
            return point;
        }

        private void HandleMouseWheel(SDLMouseWheelEvent wheel)
        {
            var pos = GetMousePos();
            wheelDeltaAccumulator += wheel.Y * 120f;
            scrollPosition = new(pos.X, pos.Y);
        }

        private void UpdateScroll()
        {
            if (host != null && Math.Abs(wheelDeltaAccumulator) > 0)
            {
                float maxScrollDelta = ScrollSpeed * ImGui.GetIO().DeltaTime;
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

            UpdateScroll();

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

            if (hovered != hoveredBefore)
            {
            }

            if (hovered && !hasFocus)
            {
                hasFocus = true;
                host?.SendFocusEvent(true);
            }
            else if (!hovered && hasFocus)
            {
                hasFocus = false;
                host?.SendFocusEvent(false);
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

            handlerBase.Draw(draw, bb);

            hoveredBefore = hovered;
        }

        public void Dispose()
        {
            Application.UnregisterHook(Hook);
            browser.Dispose();
        }
    }
}