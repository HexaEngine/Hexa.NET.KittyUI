﻿namespace Hexa.NET.Kitty.Windows
{
    using Hexa.NET.Kitty.Input;
    using Hexa.NET.Kitty.Input.Events;
    using Hexa.NET.Kitty.Logging;
    using Hexa.NET.Kitty.Windows.Events;
    using Hexa.NET.Mathematics;
    using Kitty.D3D11;
    using Kitty.Extensions;
    using Kitty.OpenGL;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Native;
    using Silk.NET.SDL;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using static Extensions.SdlErrorHandlingExtensions;
    using Key = Input.Key;

    public enum GraphicsBackend
    {
        D3D11,
        OpenGL,
        Vulkan,
        Metal
    }

    public unsafe class SdlWindow : IWindow, INativeWindow
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(Sdl));
        protected readonly Sdl sdl = Sdl.GetApi();
        private readonly ShownEventArgs shownEventArgs = new();
        private readonly HiddenEventArgs hiddenEventArgs = new();
        private readonly ExposedEventArgs exposedEventArgs = new();
        private readonly MovedEventArgs movedEventArgs = new();
        private readonly ResizedEventArgs resizedEventArgs = new();
        private readonly SizeChangedEventArgs sizeChangedEventArgs = new();
        private readonly MinimizedEventArgs minimizedEventArgs = new();
        private readonly MaximizedEventArgs maximizedEventArgs = new();
        private readonly RestoredEventArgs restoredEventArgs = new();
        private readonly EnterEventArgs enterEventArgs = new();
        private readonly LeaveEventArgs leaveEventArgs = new();
        private readonly FocusGainedEventArgs focusGainedEventArgs = new();
        private readonly FocusLostEventArgs focusLostEventArgs = new();
        private readonly CloseEventArgs closeEventArgs = new();
        private readonly TakeFocusEventArgs takeFocusEventArgs = new();
        private readonly HitTestEventArgs hitTestEventArgs = new();
        private readonly KeyboardEventArgs keyboardEventArgs = new();
        private readonly KeyboardCharEventArgs keyboardCharEventArgs = new();
        private readonly MouseButtonEventArgs mouseButtonEventArgs = new();
        private readonly MouseMotionEventArgs mouseMotionEventArgs = new();
        private readonly MouseWheelEventArgs mouseWheelEventArgs = new();
        private readonly TouchEventArgs touchEventArgs = new();
        private readonly TouchMotionEventArgs touchMotionEventArgs = new();

        private Silk.NET.SDL.Window* window;
        private bool created;
        private bool destroyed;
        private int width = 1280;
        private int height = 720;
        private int y = 100;
        private int x = 100;
        private bool hovering;
        private bool focused;
        private WindowState state;
        private string title = "Window";
        private bool lockCursor;
        private bool resizable = true;
        private bool bordered = true;

        private Cursor** cursors;

        public SdlWindow(WindowFlags flags = WindowFlags.Resizable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Kind = NativeWindowFlags.Win32;
            }
            else
            {
                Kind = NativeWindowFlags.Sdl;
            }

            PlatformConstruct(flags);
        }

        public SdlWindow(int x, int y, int width, int height, WindowFlags flags = WindowFlags.Resizable)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Kind = NativeWindowFlags.Win32;
            }
            else
            {
                Kind = NativeWindowFlags.Sdl;
            }

            PlatformConstruct(flags);
        }

        public GraphicsBackend Backend { get; private set; }

        private void PlatformConstruct(WindowFlags windowFlags)
        {
            if (created)
            {
                return;
            }
            GraphicsBackend backend = GraphicsBackend.OpenGL;
            if (OperatingSystem.IsWindows())
            {
                backend = GraphicsBackend.D3D11;
            }

#if FORCE_OPENGL
            backend = GraphicsBackend.OpenGL;
#endif

            byte[] bytes = Encoding.UTF8.GetBytes(title);
            byte* ptr = (byte*)Unsafe.AsPointer(ref bytes[0]);

            windowFlags |= WindowFlags.Hidden;

            switch (backend)
            {
                case GraphicsBackend.OpenGL:
                    windowFlags |= WindowFlags.Opengl;
                    break;

                case GraphicsBackend.Vulkan:
                    windowFlags |= WindowFlags.Vulkan;
                    break;

                case GraphicsBackend.Metal:
                    windowFlags |= WindowFlags.Metal;
                    break;
            }

            window = SdlCheckError(sdl.CreateWindow(ptr, x, y, width, height, (uint)windowFlags));

            WindowID = sdl.GetWindowID(window).SdlThrowIf();

            int w;
            int h;
            sdl.GetWindowSize(window, &w, &h);

            cursors = (Cursor**)AllocArray((uint)SystemCursor.NumSystemCursors);
            for (SystemCursor i = 0; i < SystemCursor.NumSystemCursors; i++)
            {
                cursors[(int)i] = SdlCheckError(sdl.CreateSystemCursor(SystemCursor.SystemCursorArrow));
            }

            Width = w;
            Height = h;
            Viewport = new(0, 0, w, h, 0, 1);
            created = true;
            destroyed = false;

            Backend = backend;

            InitGraphics(backend);
        }

        private void InitGraphics(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.D3D11:
                    D3D11Adapter.Init(this, false);
                    break;

                case GraphicsBackend.OpenGL:
                    sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
                    sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
                    sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.Core);

                    OpenGLAdapter.Init(this);
                    break;

                default:
                    throw new NotSupportedException("The specified graphics backend is not supported");
            }
        }

        public void Show()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            Application.RegisterWindow((IRenderWindow)this);
            sdl.ShowWindow(window);
        }

        public void Hide()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            sdl.HideWindow(window);
            OnHidden(hiddenEventArgs);
        }

        public void Close()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            closeEventArgs.Handled = false;
            OnClosing(closeEventArgs);
        }

        public void ReleaseCapture()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            sdl.CaptureMouse(SdlBool.False);
        }

        public void Capture()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            sdl.CaptureMouse(SdlBool.True);
        }

        public void Fullscreen(FullscreenMode mode)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            sdl.SetWindowFullscreen(window, (uint)mode);
        }

        [SupportedOSPlatform("windows")]
        public nint GetHWND()
        {
            SysWMInfo wmInfo;
            sdl.GetVersion(&wmInfo.Version);
            sdl.GetWindowWMInfo(window, &wmInfo);
            return wmInfo.Info.Win.Hwnd;
        }

        public bool VulkanCreateSurface(VkHandle vkHandle, VkNonDispatchableHandle* vkNonDispatchableHandle)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            return sdl.VulkanCreateSurface(window, vkHandle, vkNonDispatchableHandle) == SdlBool.True;
        }

        public IGLContext OpenGLCreateContext()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            return new SdlContext(sdl, window, null, (GLattr.ContextMajorVersion, 4), (GLattr.ContextMinorVersion, 5));
        }

        public Silk.NET.SDL.Window* GetWindow() => window;

        public uint WindowID { get; private set; }

        public string Title
        {
            get => title;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                title = value;
                sdl.SetWindowTitle(window, value);
            }
        }

        public int X
        {
            get => x;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                x = value;
                sdl.SetWindowPosition(window, value, y);
            }
        }

        public int Y
        {
            get => y;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                y = value;
                sdl.SetWindowPosition(window, x, value);
            }
        }

        public int Width
        {
            get => width;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                resizedEventArgs.OldWidth = width;
                resizedEventArgs.NewWidth = value;
                width = value;
                sdl.SetWindowSize(window, value, height);
                Viewport = new(width, height);
                OnResized(resizedEventArgs);
            }
        }

        public int Height
        {
            get => height;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                resizedEventArgs.OldHeight = height;
                resizedEventArgs.NewHeight = value;
                height = value;
                sdl.SetWindowSize(window, width, value);
                Viewport = new(width, height);
                OnResized(resizedEventArgs);
            }
        }

        public Rectangle BorderSize
        {
            get
            {
                Rectangle result;
                sdl.GetWindowBordersSize(window, &result.Top, &result.Left, &result.Bottom, &result.Right);
                return result;
            }
        }

        public bool Hovering => hovering;

        public bool Focused => focused;

        public WindowState State
        {
            get => state;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                state = value;
                switch (value)
                {
                    case WindowState.Hidden:
                        sdl.HideWindow(window);
                        break;

                    case WindowState.Normal:
                        sdl.ShowWindow(window);
                        break;

                    case WindowState.Minimized:
                        sdl.MinimizeWindow(window);
                        break;

                    case WindowState.Maximized:
                        sdl.MaximizeWindow(window);
                        break;
                }
            }
        }

        public bool LockCursor
        {
            get => lockCursor;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                lockCursor = value;
                sdl.SetRelativeMouseMode(value ? SdlBool.True : SdlBool.False);
            }
        }

        public bool Resizable
        {
            get => resizable;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                resizable = value;
                sdl.SetWindowResizable(window, value ? SdlBool.True : SdlBool.False);
            }
        }

        public bool Bordered
        {
            get => bordered;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                bordered = value;
                sdl.SetWindowBordered(window, value ? SdlBool.True : SdlBool.False);
            }
        }

        public Viewport Viewport { get; private set; }

        public NativeWindowFlags Kind { get; }

        public (nint Display, nuint Window)? X11 { get; }

        public nint? Cocoa { get; }

        public (nint Display, nint Surface)? Wayland { get; }

        public nint? WinRT { get; }

        public (nint Window, uint Framebuffer, uint Colorbuffer, uint ResolveFramebuffer)? UIKit { get; }

        public (nint Hwnd, nint HDC, nint HInstance)? Win32
        {
            get
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                SysWMInfo wmInfo;
                sdl.GetVersion(&wmInfo.Version);
                sdl.GetWindowWMInfo(window, &wmInfo);

                return (wmInfo.Info.Win.Hwnd, wmInfo.Info.Win.HDC, wmInfo.Info.Win.HInstance);
            }
        }

        public (nint Display, nint Window)? Vivante { get; }

        public (nint Window, nint Surface)? Android { get; }

        public nint? Glfw { get; }

        nint? INativeWindow.Sdl => (nint?)window;

        public nint? DXHandle { get; }

        public (nint? Display, nint? Surface)? EGL { get; }

        public INativeWindow? Native => this;

        #region Events

        /// <summary>
        /// Event triggered when the window is shown.
        /// </summary>
        public event EventHandler<ShownEventArgs>? Shown;

        /// <summary>
        /// Event triggered when the window is hidden.
        /// </summary>
        public event EventHandler<HiddenEventArgs>? Hidden;

        /// <summary>
        /// Event triggered when the window is exposed.
        /// </summary>
        public event EventHandler<ExposedEventArgs>? Exposed;

        /// <summary>
        /// Event triggered when the window is moved.
        /// </summary>
        public event EventHandler<MovedEventArgs>? Moved;

        /// <summary>
        /// Event triggered when the window is resized.
        /// </summary>
        public event EventHandler<ResizedEventArgs>? Resized;

        /// <summary>
        /// Event triggered when the window size is changed.
        /// </summary>
        public event EventHandler<SizeChangedEventArgs>? SizeChanged;

        /// <summary>
        /// Event triggered when the window is minimized.
        /// </summary>
        public event EventHandler<MinimizedEventArgs>? Minimized;

        /// <summary>
        /// Event triggered when the window is maximized.
        /// </summary>
        public event EventHandler<MaximizedEventArgs>? Maximized;

        /// <summary>
        /// Event triggered when the window is restored.
        /// </summary>
        public event EventHandler<RestoredEventArgs>? Restored;

        /// <summary>
        /// Event triggered when the mouse enters the window.
        /// </summary>
        public event EventHandler<EnterEventArgs>? Enter;

        /// <summary>
        /// Event triggered when the mouse leaves the window.
        /// </summary>
        public event EventHandler<LeaveEventArgs>? Leave;

        /// <summary>
        /// Event triggered when the window gains focus.
        /// </summary>
        public event EventHandler<FocusGainedEventArgs>? FocusGained;

        /// <summary>
        /// Event triggered when the window loses focus.
        /// </summary>
        public event EventHandler<FocusLostEventArgs>? FocusLost;

        /// <summary>
        /// Event triggered when the window is closing.
        /// </summary>
        public event EventHandler<CloseEventArgs>? Closing;

        /// <summary>
        /// Event triggered when the window is closed.
        /// </summary>
        public event EventHandler<CloseEventArgs>? Closed;

        /// <summary>
        /// Event triggered when the window requests to take focus.
        /// </summary>
        public event EventHandler<TakeFocusEventArgs>? TakeFocus;

        /// <summary>
        /// Event triggered when a hit test is performed on the window.
        /// </summary>
        public event EventHandler<HitTestEventArgs>? HitTest;

        /// <summary>
        /// Event triggered when a keyboard input is received.
        /// </summary>
        public event EventHandler<KeyboardEventArgs>? KeyboardInput;

        /// <summary>
        /// Event triggered when a character input is received from the keyboard.
        /// </summary>
        public event EventHandler<KeyboardCharEventArgs>? KeyboardCharInput;

        /// <summary>
        /// Event triggered when a mouse button input is received.
        /// </summary>
        public event EventHandler<MouseButtonEventArgs>? MouseButtonInput;

        /// <summary>
        /// Event triggered when a mouse motion input is received.
        /// </summary>
        public event EventHandler<MouseMotionEventArgs>? MouseMotionInput;

        /// <summary>
        /// Event triggered when a mouse wheel input is received.
        /// </summary>
        public event EventHandler<MouseWheelEventArgs>? MouseWheelInput;

        /// <summary>
        /// Event triggered when a touch input is received.
        /// </summary>
        public event EventHandler<TouchEventArgs>? TouchInput;

        /// <summary>
        /// Event triggered when a touch motion input is received.
        /// </summary>
        public event EventHandler<TouchMotionEventArgs>? TouchMotionInput;

        #endregion Events

        #region EventCallMethods

        /// <summary>
        /// Raises the <see cref="Shown"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnShown(ShownEventArgs args)
        {
            Shown?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Hidden"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnHidden(HiddenEventArgs args)
        {
            Hidden?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Exposed"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnExposed(ExposedEventArgs args)
        {
            Exposed?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Moved"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMoved(MovedEventArgs args)
        {
            Moved?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Resized"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnResized(ResizedEventArgs args)
        {
            Resized?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="SizeChanged"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnSizeChanged(SizeChangedEventArgs args)
        {
            SizeChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Minimized"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMinimized(MinimizedEventArgs args)
        {
            Minimized?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Maximized"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMaximized(MaximizedEventArgs args)
        {
            Maximized?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Restored"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnRestored(RestoredEventArgs args)
        {
            Restored?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Enter"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnEnter(EnterEventArgs args)
        {
            Enter?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Leave"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnLeave(LeaveEventArgs args)
        {
            Leave?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="FocusGained"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnFocusGained(FocusGainedEventArgs args)
        {
            FocusGained?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="FocusLost"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnFocusLost(FocusLostEventArgs args)
        {
            FocusLost?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnClosing(CloseEventArgs args)
        {
            Closing?.Invoke(this, args);
            if (!args.Handled && !destroyed)
            {
                DestroyWindow();
            }
            else
            {
                Application.SuppressQuitApp();
            }
        }

        /// <summary>
        /// Raises the <see cref="Closed"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnClosed(CloseEventArgs args)
        {
            DestroyWindow();

            Closed?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="TakeFocus"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnTakeFocus(TakeFocusEventArgs args)
        {
            TakeFocus?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="HitTest"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnHitTest(HitTestEventArgs args)
        {
            HitTest?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="KeyboardInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnKeyboardInput(KeyboardEventArgs args)
        {
            KeyboardInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="KeyboardCharInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnKeyboardCharInput(KeyboardCharEventArgs args)
        {
            KeyboardCharInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="MouseButtonInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMouseButtonInput(MouseButtonEventArgs args)
        {
            MouseButtonInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="MouseMotionInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMouseMotionInput(MouseMotionEventArgs args)
        {
            MouseMotionInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="MouseWheelInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnMouseWheelInput(MouseWheelEventArgs args)
        {
            MouseWheelInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="TouchInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnTouchInput(TouchEventArgs args)
        {
            TouchInput?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the <see cref="TouchMotionInput"/> event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnTouchMotionInput(TouchMotionEventArgs args)
        {
            TouchMotionInput?.Invoke(this, args);
        }

        #endregion EventCallMethods

        /// <summary>
        /// Processes a window event received from the message loop.
        /// </summary>
        /// <param name="evnt">The window event to process.</param>
        internal void ProcessEvent(WindowEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            WindowEventID type = (WindowEventID)evnt.Event;
            switch (type)
            {
                case WindowEventID.None:
                    return;

                case WindowEventID.Shown:
                    {
                        shownEventArgs.Timestamp = evnt.Timestamp;
                        shownEventArgs.Handled = false;
                        OnShown(shownEventArgs);
                        if (shownEventArgs.Handled)
                        {
                            sdl.HideWindow(window);
                        }
                    }
                    break;

                case WindowEventID.Hidden:
                    {
                        WindowState oldState = state;
                        state = WindowState.Hidden;
                        hiddenEventArgs.Timestamp = evnt.Timestamp;
                        hiddenEventArgs.OldState = oldState;
                        hiddenEventArgs.NewState = WindowState.Hidden;
                        hiddenEventArgs.Handled = false;
                        OnHidden(hiddenEventArgs);
                        if (hiddenEventArgs.Handled)
                        {
                            sdl.ShowWindow(window);
                        }
                    }
                    break;

                case WindowEventID.Exposed:
                    {
                        exposedEventArgs.Timestamp = evnt.Timestamp;
                        exposedEventArgs.Handled = false;
                        OnExposed(exposedEventArgs);
                    }
                    break;

                case WindowEventID.Moved:
                    {
                        int xold = x;
                        int yold = y;
                        x = evnt.Data1;
                        y = evnt.Data2;
                        movedEventArgs.Timestamp = evnt.Timestamp;
                        movedEventArgs.OldX = xold;
                        movedEventArgs.OldY = yold;
                        movedEventArgs.NewX = x;
                        movedEventArgs.NewY = y;
                        movedEventArgs.Handled = false;
                        OnMoved(movedEventArgs);
                        if (movedEventArgs.Handled)
                        {
                            sdl.SetWindowPosition(window, xold, yold);
                        }
                    }
                    break;

                case WindowEventID.Resized:
                    {
                        int widthOld = width;
                        int heightOld = height;
                        width = evnt.Data1;
                        height = evnt.Data2;
                        Viewport = new(width, height);
                        resizedEventArgs.Timestamp = evnt.Timestamp;
                        resizedEventArgs.OldWidth = widthOld;
                        resizedEventArgs.OldWidth = heightOld;
                        resizedEventArgs.NewWidth = width;
                        resizedEventArgs.NewHeight = height;
                        resizedEventArgs.Handled = false;
                        OnResized(resizedEventArgs);
                        if (resizedEventArgs.Handled)
                        {
                            sdl.SetWindowSize(window, widthOld, heightOld);
                        }
                    }
                    break;

                case WindowEventID.SizeChanged:
                    {
                        int widthOld = width;
                        int heightOld = height;
                        width = evnt.Data1;
                        height = evnt.Data2;
                        Viewport = new(width, height);
                        sizeChangedEventArgs.Timestamp = evnt.Timestamp;
                        sizeChangedEventArgs.OldWidth = widthOld;
                        sizeChangedEventArgs.OldHeight = heightOld;
                        sizeChangedEventArgs.Width = evnt.Data1;
                        sizeChangedEventArgs.Height = evnt.Data2;
                        sizeChangedEventArgs.Handled = false;
                        OnSizeChanged(sizeChangedEventArgs);
                    }
                    break;

                case WindowEventID.Minimized:
                    {
                        WindowState oldState = state;
                        state = WindowState.Minimized;
                        minimizedEventArgs.Timestamp = evnt.Timestamp;
                        minimizedEventArgs.OldState = oldState;
                        minimizedEventArgs.NewState = WindowState.Minimized;
                        minimizedEventArgs.Handled = false;
                        OnMinimized(minimizedEventArgs);
                        if (minimizedEventArgs.Handled)
                        {
                            State = oldState;
                        }
                    }
                    break;

                case WindowEventID.Maximized:
                    {
                        WindowState oldState = state;
                        state = WindowState.Maximized;
                        maximizedEventArgs.Timestamp = evnt.Timestamp;
                        maximizedEventArgs.OldState = oldState;
                        maximizedEventArgs.NewState = WindowState.Maximized;
                        maximizedEventArgs.Handled = false;
                        OnMaximized(maximizedEventArgs);
                        if (maximizedEventArgs.Handled)
                        {
                            State = oldState;
                        }
                    }
                    break;

                case WindowEventID.Restored:
                    {
                        WindowState oldState = state;
                        state = WindowState.Normal;
                        restoredEventArgs.Timestamp = evnt.Timestamp;
                        restoredEventArgs.OldState = oldState;
                        restoredEventArgs.NewState = WindowState.Normal;
                        restoredEventArgs.Handled = false;
                        OnRestored(restoredEventArgs);
                        if (restoredEventArgs.Handled)
                        {
                            State = oldState;
                        }
                    }
                    break;

                case WindowEventID.Enter:
                    {
                        hovering = true;
                        enterEventArgs.Timestamp = evnt.Timestamp;
                        enterEventArgs.Handled = false;
                        OnEnter(enterEventArgs);
                    }
                    break;

                case WindowEventID.Leave:
                    {
                        hovering = false;
                        leaveEventArgs.Timestamp = evnt.Timestamp;
                        leaveEventArgs.Handled = false;
                        OnLeave(leaveEventArgs);
                    }
                    break;

                case WindowEventID.FocusGained:
                    {
                        focused = true;
                        focusGainedEventArgs.Timestamp = evnt.Timestamp;
                        focusGainedEventArgs.Handled = false;
                        OnFocusGained(focusGainedEventArgs);
                    }
                    break;

                case WindowEventID.FocusLost:
                    {
                        focused = false;
                        focusLostEventArgs.Timestamp = evnt.Timestamp;
                        focusLostEventArgs.Handled = false;
                        OnFocusLost(focusLostEventArgs);
                    }
                    break;

                case WindowEventID.Close:
                    {
                        closeEventArgs.Timestamp = evnt.Timestamp;
                        closeEventArgs.Handled = false;
                        OnClosing(closeEventArgs);
                        if (closeEventArgs.Handled)
                        {
                            sdl.ShowWindow(window);
                        }
                    }
                    break;

                case WindowEventID.TakeFocus:
                    {
                        takeFocusEventArgs.Timestamp = evnt.Timestamp;
                        takeFocusEventArgs.Handled = false;
                        OnTakeFocus(takeFocusEventArgs);
                        if (!takeFocusEventArgs.Handled)
                        {
                            sdl.SetWindowInputFocus(window).SdlThrowIf();
                        }
                    }
                    break;

                case WindowEventID.HitTest:
                    {
                        hitTestEventArgs.Timestamp = evnt.Timestamp;
                        hitTestEventArgs.Handled = false;
                        OnHitTest(hitTestEventArgs);
                    }
                    break;
            }
        }

        /// <summary>
        /// Processes a keyboard input event received from the message loop.
        /// </summary>
        /// <param name="evnt">The keyboard event to process.</param>
        internal void ProcessInputKeyboard(KeyboardEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            KeyState state = (KeyState)evnt.State;
            Key keyCode = (Key)sdl.GetKeyFromScancode(evnt.Keysym.Scancode);
            keyboardEventArgs.Timestamp = evnt.Timestamp;
            keyboardEventArgs.Handled = false;
            keyboardEventArgs.State = state;
            keyboardEventArgs.KeyCode = keyCode;
            keyboardEventArgs.ScanCode = (ScanCode)evnt.Keysym.Scancode;
            OnKeyboardInput(keyboardEventArgs);
        }

        /// <summary>
        /// Processes a text input event received from the message loop.
        /// </summary>
        /// <param name="evnt">The text input event to process.</param>
        internal void ProcessInputText(TextInputEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            keyboardCharEventArgs.Timestamp = evnt.Timestamp;
            keyboardCharEventArgs.Handled = false;
            keyboardCharEventArgs.Char = (char)evnt.Text[0];
            OnKeyboardCharInput(keyboardCharEventArgs);
        }

        /// <summary>
        /// Processes a mouse button event received from the message loop.
        /// </summary>
        /// <param name="evnt">The mouse button event to process.</param>
        internal void ProcessInputMouse(MouseButtonEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            MouseButtonState state = (MouseButtonState)evnt.State;
            MouseButton button = (MouseButton)evnt.Button;
            mouseButtonEventArgs.Timestamp = evnt.Timestamp;
            mouseButtonEventArgs.Handled = false;
            mouseButtonEventArgs.MouseId = evnt.Which;
            mouseButtonEventArgs.Button = button;
            mouseButtonEventArgs.State = state;
            mouseButtonEventArgs.Clicks = evnt.Clicks;
            OnMouseButtonInput(mouseButtonEventArgs);
        }

        /// <summary>
        /// Processes a mouse motion event received from the message loop.
        /// </summary>
        /// <param name="evnt">The mouse motion event to process.</param>
        internal void ProcessInputMouse(MouseMotionEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            if (lockCursor)
            {
                sdl.WarpMouseInWindow(window, 0, 0);
            }

            mouseMotionEventArgs.Timestamp = evnt.Timestamp;
            mouseMotionEventArgs.Handled = false;
            mouseMotionEventArgs.MouseId = evnt.Which;
            mouseMotionEventArgs.X = evnt.X;
            mouseMotionEventArgs.Y = evnt.Y;
            mouseMotionEventArgs.RelX = evnt.Xrel;
            mouseMotionEventArgs.RelY = evnt.Yrel;
            OnMouseMotionInput(mouseMotionEventArgs);
        }

        /// <summary>
        /// Processes a mouse wheel event received from the message loop.
        /// </summary>
        /// <param name="evnt">The mouse wheel event to process.</param>
        internal void ProcessInputMouse(MouseWheelEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            mouseWheelEventArgs.Timestamp = evnt.Timestamp;
            mouseWheelEventArgs.Handled = false;
            mouseWheelEventArgs.MouseId = evnt.Which;
            mouseWheelEventArgs.Wheel = new(evnt.X, evnt.Y);
            mouseWheelEventArgs.Direction = (Input.MouseWheelDirection)evnt.Direction;
            OnMouseWheelInput(mouseWheelEventArgs);
        }

        internal void ProcessInputTouchMotion(TouchFingerEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            touchMotionEventArgs.Timestamp = evnt.Timestamp;
            touchMotionEventArgs.TouchDeviceId = evnt.TouchId;
            touchMotionEventArgs.FingerId = evnt.FingerId;
            touchMotionEventArgs.Pressure = evnt.Pressure;
            touchMotionEventArgs.X = evnt.X;
            touchMotionEventArgs.Y = evnt.Y;
            touchMotionEventArgs.Dx = evnt.Dx;
            touchMotionEventArgs.Dy = evnt.Dy;
            OnTouchMotionInput(touchMotionEventArgs);
        }

        internal void ProcessInputTouchUp(TouchFingerEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            touchEventArgs.Timestamp = evnt.Timestamp;
            touchEventArgs.TouchDeviceId = evnt.TouchId;
            touchEventArgs.FingerId = evnt.FingerId;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Up;
            OnTouchInput(touchEventArgs);
        }

        internal void ProcessInputTouchDown(TouchFingerEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            touchEventArgs.Timestamp = evnt.Timestamp;
            touchEventArgs.TouchDeviceId = evnt.TouchId;
            touchEventArgs.FingerId = evnt.FingerId;
            touchEventArgs.Pressure = evnt.Pressure;
            touchEventArgs.X = evnt.X;
            touchEventArgs.Y = evnt.Y;
            touchEventArgs.State = FingerState.Down;
            OnTouchInput(touchEventArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearState()
        {
            Keyboard.Flush();
            Mouse.Flush();
        }

        internal void DestroyWindow()
        {
            if (!destroyed)
            {
                switch (Backend)
                {
                    case GraphicsBackend.D3D11:
                        D3D11Adapter.Shutdown();
                        break;

                    case GraphicsBackend.OpenGL:
                        OpenGLAdapter.Shutdown();
                        break;
                }
                if (cursors != null)
                {
                    for (SystemCursor i = 0; i < SystemCursor.NumSystemCursors; i++)
                    {
                        sdl.FreeCursor(cursors[(int)i]);
                    }
                    Free(cursors);
                    cursors = null;
                }

                if (window != null)
                {
                    sdl.DestroyWindow(window);
                    SdlCheckError();
                    window = null;
                }

                destroyed = true;
                created = false;
            }
        }
    }
}