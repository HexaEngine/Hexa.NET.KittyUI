namespace Hexa.NET.KittyUI.Windows
{
    using Hexa.NET.KittyUI;
    using Hexa.NET.KittyUI.D3D11;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.Input.Events;
    using Hexa.NET.KittyUI.OpenGL;
    using Hexa.NET.KittyUI.UI;
    using Hexa.NET.KittyUI.Windows.Events;
    using Hexa.NET.Logging;
    using Hexa.NET.Mathematics;
    using Hexa.NET.SDL2;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using static Hexa.NET.KittyUI.Extensions.SdlErrorHandlingExtensions;
    using Key = Input.Key;

    public unsafe class CoreWindow : IWindow
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(SDL));

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

        private ITitleBar? titlebar;

        private SDLWindow* window;
        private bool created;
        private bool destroyed;
        private int width = 1280;
        private int height = 720;
        private int y = (int)SDL.SDL_WINDOWPOS_UNDEFINED_MASK;
        private int x = (int)SDL.SDL_WINDOWPOS_UNDEFINED_MASK;
        private bool hovering;
        private bool focused;
        private WindowState state;
        private string title = "Window";
        private bool lockCursor;
        private bool resizable = true;
        private bool bordered = true;

        private SDLCursor** cursors;

        public CoreWindow(SDLWindowFlags flags = SDLWindowFlags.Resizable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
            }
            else
            {
            }

            PlatformConstruct(flags);
        }

        public CoreWindow(int x, int y, int width, int height, SDLWindowFlags flags = SDLWindowFlags.Resizable)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
            }
            else
            {
            }

            PlatformConstruct(flags);
        }

        public GraphicsBackend Backend { get; private set; }

        private void PlatformConstruct(SDLWindowFlags windowFlags)
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

            if (Application.SelectedGraphicsBackend != GraphicsBackend.Auto)
            {
                backend = Application.SelectedGraphicsBackend;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(title);
            byte* ptr = (byte*)Unsafe.AsPointer(ref bytes[0]);

            windowFlags |= SDLWindowFlags.Hidden | SDLWindowFlags.AllowHighdpi;

            switch (backend)
            {
                case GraphicsBackend.OpenGL:
                    windowFlags |= SDLWindowFlags.Opengl;
                    break;

                case GraphicsBackend.Vulkan:
                    windowFlags |= SDLWindowFlags.Vulkan;
                    break;

                case GraphicsBackend.Metal:
                    windowFlags |= SDLWindowFlags.Metal;
                    break;
            }

            window = SdlCheckError(SDL.CreateWindow(ptr, (int)x, (int)y, (int)width, (int)height, (uint)windowFlags));

            WindowID = SDL.GetWindowID(window).SdlThrowIf();

            int w;
            int h;
            SDL.GetWindowSize(window, &w, &h);

            cursors = (SDLCursor**)AllocArray((uint)SDLSystemCursor.NumSystemCursors);
            for (SDLSystemCursor i = 0; i < SDLSystemCursor.NumSystemCursors; i++)
            {
                cursors[(int)i] = SdlCheckError(SDL.CreateSystemCursor(SDLSystemCursor.Arrow));
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
                    if (OperatingSystem.IsWindows())
                    {
                        D3D11Adapter.Init(this, true); break;
                    }

                    goto default;

                case GraphicsBackend.OpenGL:
                    if (OperatingSystem.IsMacOS())
                    {
                        // Set max OpenGL version to 4.1 for macOS
                        SDL.GLSetAttribute(SDLGLattr.GlContextMajorVersion, 4);
                        SDL.GLSetAttribute(SDLGLattr.GlContextMinorVersion, 1);
                    }
                    else
                    {
                        // Set to OpenGL 4.5 for other platforms
                        SDL.GLSetAttribute(SDLGLattr.GlContextMajorVersion, 4);
                        SDL.GLSetAttribute(SDLGLattr.GlContextMinorVersion, 5);
                    }
                    SDL.GLSetAttribute(SDLGLattr.GlContextProfileMask, (int)SDLGLprofile.GlContextProfileCore);

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
            SDL.ShowWindow(window);
        }

        public void Hide()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            SDL.HideWindow(window);
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
            SDL.CaptureMouse(SDLBool.False);
        }

        public void Capture()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            SDL.CaptureMouse(SDLBool.True);
        }

        public void Fullscreen(FullscreenMode mode)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            SDL.SetWindowFullscreen(window, (uint)mode);
        }

        [SupportedOSPlatform("windows")]
        public nint GetHWND()
        {
            SDLSysWMInfo wmInfo;
            SDL.GetVersion(&wmInfo.Version);
            SDL.GetWindowWMInfo(window, &wmInfo);
            return wmInfo.Info.Win.Window;
        }

        //public bool VulkanCreateSurface(VkHandle vkHandle, VkNonDispatchableHandle* vkNonDispatchableHandle)
        //{
        //    Logger.ThrowIf(destroyed, "The window is already destroyed");
        //    return SDL.VulkanCreateSurface(window, vkHandle.Handle, (VkSurfaceKHR*)vkNonDispatchableHandle) == SDLBool.True;
        //}

        public IGLContext OpenGLCreateContext()
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            return new SdlContext(window);
        }

        public SDLWindow* GetWindow() => window;

        public uint WindowID { get; private set; }

        public string Title
        {
            get => title;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                title = value;
                SDL.SetWindowTitle(window, value);
            }
        }

        public int X
        {
            get => x;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                x = value;
                SDL.SetWindowPosition(window, value, y);
            }
        }

        public int Y
        {
            get => y;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                y = value;
                SDL.SetWindowPosition(window, x, value);
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
                SDL.SetWindowSize(window, value, height);
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
                SDL.SetWindowSize(window, width, value);
                Viewport = new(width, height);
                OnResized(resizedEventArgs);
            }
        }

        public ITitleBar? TitleBar
        {
            get => titlebar;
            set
            {
                if (titlebar != null)
                {
                    DetatchTitlebar(titlebar);
                }

                if (value != null)
                {
                    AttachTitlebar(value);
                }
                else
                {
                }

                titlebar = value;
            }
        }

        public Rect BorderSize
        {
            get
            {
                Rect result;
                SDL.GetWindowBordersSize(window, (int*)&result.Top, (int*)&result.Left, (int*)&result.Bottom, (int*)&result.Right);
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
                        SDL.HideWindow(window);
                        break;

                    case WindowState.Normal:
                        SDL.ShowWindow(window);
                        break;

                    case WindowState.Minimized:
                        SDL.MinimizeWindow(window);
                        break;

                    case WindowState.Maximized:
                        SDL.MaximizeWindow(window);
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
                SDL.SetRelativeMouseMode(value ? SDLBool.True : SDLBool.False);
            }
        }

        public bool Resizable
        {
            get => resizable;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                resizable = value;
                SDL.SetWindowResizable(window, value ? SDLBool.True : SDLBool.False);
            }
        }

        public bool Bordered
        {
            get => bordered;
            set
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                bordered = value;
                SDL.SetWindowBordered(window, value ? SDLBool.True : SDLBool.False);
            }
        }

        public Viewport Viewport { get; private set; }

        public (nint Display, nuint Window)? X11
        {
            get
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                SDLSysWMInfo wmInfo;
                SDL.GetVersion(&wmInfo.Version);
                SDL.GetWindowWMInfo(window, &wmInfo);

                return (wmInfo.Info.X11.Display, (nuint)wmInfo.Info.X11.Window);
            }
        }

        public nint? Cocoa { get; }

        public (nint Display, nint Surface)? Wayland
        {
            get
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                SDLSysWMInfo wmInfo;
                SDL.GetVersion(&wmInfo.Version);
                SDL.GetWindowWMInfo(window, &wmInfo);

                return (wmInfo.Info.Wayland.Display, wmInfo.Info.Wayland.Surface);
            }
        }

        public nint? WinRT { get; }

        public (nint Window, uint Framebuffer, uint Colorbuffer, uint ResolveFramebuffer)? UIKit { get; }

        public (nint Hwnd, nint HDC, nint HInstance)? Win32
        {
            get
            {
                Logger.ThrowIf(destroyed, "The window is already destroyed");
                SDLSysWMInfo wmInfo;
                SDL.GetVersion(&wmInfo.Version);
                SDL.GetWindowWMInfo(window, &wmInfo);

                return (wmInfo.Info.Win.Window, wmInfo.Info.Win.Hdc, wmInfo.Info.Win.HInstance);
            }
        }

        public (nint Display, nint Window)? Vivante { get; }

        public (nint Window, nint Surface)? Android { get; }

        public nint? Glfw { get; }

        public nint? Sdl => (nint?)window;

        public nint? DXHandle { get; }

        public (nint? Display, nint? Surface)? EGL { get; }

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
                OnClosed(args);
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

        private SDLHitTest callback;

        private void AttachTitlebar(ITitleBar titlebar)
        {
            titlebar.CloseWindowRequest += OnTitleBarCloseWindowRequest;
            titlebar.MinimizeWindowRequest += OnTitleBarMinimizeWindowRequest;
            titlebar.MaximizeWindowRequest += OnTitleBarMaximizeWindowRequest;
            titlebar.RestoreWindowRequest += OnTitlebarRestoreWindowRequest;

            callback = HitTestCallback;
            SDL.SetWindowHitTest(window, callback, null);
            titlebar.OnAttach(this);
        }

        private SDLHitTestResult HitTestCallback(SDLWindow* win, SDLPoint* area, void* data)
        {
            var titlebarHeight = titlebar!.Height;
            int mouseGrabPadding = 4;

            if (state != WindowState.Normal) // remove padding on maximized state.
            {
                mouseGrabPadding = 0;
            }

            if (area->Y < mouseGrabPadding)
            {
                if (area->X < mouseGrabPadding)
                {
                    return SDLHitTestResult.ResizeTopleft;
                }
                else if (area->X > Width - mouseGrabPadding)
                {
                    return SDLHitTestResult.ResizeTopright;
                }
                else
                {
                    return SDLHitTestResult.ResizeTop;
                }
            }
            else if (area->Y > Height - mouseGrabPadding)
            {
                if (area->X < mouseGrabPadding)
                {
                    return SDLHitTestResult.ResizeBottomleft;
                }
                else if (area->X > Width - mouseGrabPadding)
                {
                    return SDLHitTestResult.ResizeBottomright;
                }
                else
                {
                    return SDLHitTestResult.ResizeBottom;
                }
            }
            else if (area->X < mouseGrabPadding)
            {
                return SDLHitTestResult.ResizeLeft;
            }
            else if (area->X > Width - mouseGrabPadding)
            {
                return SDLHitTestResult.ResizeRight;
            }
            else if (area->Y < titlebarHeight)
            {
                return titlebar.HitTest(win, area, data);
            }

            return SDLHitTestResult.Normal; // SDL_HITTEST_NORMAL <- Windows behaviour
        }

        private void DetatchTitlebar(ITitleBar titlebar)
        {
            titlebar.OnDetach(this);
            titlebar.CloseWindowRequest -= OnTitleBarCloseWindowRequest;
            titlebar.MinimizeWindowRequest -= OnTitleBarMinimizeWindowRequest;
            titlebar.MaximizeWindowRequest -= OnTitleBarMaximizeWindowRequest;
            titlebar.RestoreWindowRequest -= OnTitlebarRestoreWindowRequest;
        }

        protected virtual void OnTitlebarRestoreWindowRequest(object? sender, RestoreWindowRequest e)
        {
            SDL.RestoreWindow(window);
            state = WindowState.Normal;
        }

        protected virtual void OnTitleBarMaximizeWindowRequest(object? sender, MaximizeWindowRequest e)
        {
            SDL.MaximizeWindow(window);
            state = WindowState.Maximized;
        }

        protected virtual void OnTitleBarMinimizeWindowRequest(object? sender, MinimizeWindowRequest e)
        {
            SDL.MinimizeWindow(window);
            state = WindowState.Minimized;
        }

        protected virtual void OnTitleBarCloseWindowRequest(object? sender, CloseWindowRequest e)
        {
            closeEventArgs.Handled = false;
            SDL.HideWindow(window);
            OnClosing(closeEventArgs);
            if (closeEventArgs.Handled)
            {
                SDL.ShowWindow(window);
            }
        }

        /// <summary>
        /// Processes a window event received from the message loop.
        /// </summary>
        /// <param name="evnt">The window event to process.</param>
        internal void ProcessEvent(SDLWindowEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            SDLWindowEventID type = (SDLWindowEventID)evnt.Event;
            switch (type)
            {
                case SDLWindowEventID.None:
                    return;

                case SDLWindowEventID.Shown:
                    {
                        state = WindowState.Normal;
                        shownEventArgs.Timestamp = evnt.Timestamp;
                        shownEventArgs.Handled = false;
                        OnShown(shownEventArgs);
                        if (shownEventArgs.Handled)
                        {
                            SDL.HideWindow(window);
                        }
                    }
                    break;

                case SDLWindowEventID.Hidden:
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
                            SDL.ShowWindow(window);
                        }
                    }
                    break;

                case SDLWindowEventID.Exposed:
                    {
                        exposedEventArgs.Timestamp = evnt.Timestamp;
                        exposedEventArgs.Handled = false;
                        OnExposed(exposedEventArgs);
                    }
                    break;

                case SDLWindowEventID.Moved:
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
                            SDL.SetWindowPosition(window, xold, yold);
                        }
                    }
                    break;

                case SDLWindowEventID.Resized:
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
                            SDL.SetWindowSize(window, widthOld, heightOld);
                        }
                    }
                    break;

                case SDLWindowEventID.SizeChanged:
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

                case SDLWindowEventID.Minimized:
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

                case SDLWindowEventID.Maximized:
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

                case SDLWindowEventID.Restored:
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

                case SDLWindowEventID.Enter:
                    {
                        hovering = true;
                        enterEventArgs.Timestamp = evnt.Timestamp;
                        enterEventArgs.Handled = false;
                        OnEnter(enterEventArgs);
                    }
                    break;

                case SDLWindowEventID.Leave:
                    {
                        hovering = false;
                        leaveEventArgs.Timestamp = evnt.Timestamp;
                        leaveEventArgs.Handled = false;
                        OnLeave(leaveEventArgs);
                    }
                    break;

                case SDLWindowEventID.FocusGained:
                    {
                        focused = true;
                        focusGainedEventArgs.Timestamp = evnt.Timestamp;
                        focusGainedEventArgs.Handled = false;
                        OnFocusGained(focusGainedEventArgs);
                    }
                    break;

                case SDLWindowEventID.FocusLost:
                    {
                        focused = false;
                        focusLostEventArgs.Timestamp = evnt.Timestamp;
                        focusLostEventArgs.Handled = false;
                        OnFocusLost(focusLostEventArgs);
                    }
                    break;

                case SDLWindowEventID.Close:
                    {
                        closeEventArgs.Timestamp = evnt.Timestamp;
                        closeEventArgs.Handled = false;
                        OnClosing(closeEventArgs);
                        if (closeEventArgs.Handled)
                        {
                            SDL.ShowWindow(window);
                        }
                    }
                    break;

                case SDLWindowEventID.TakeFocus:
                    {
                        takeFocusEventArgs.Timestamp = evnt.Timestamp;
                        takeFocusEventArgs.Handled = false;
                        OnTakeFocus(takeFocusEventArgs);
                        if (!takeFocusEventArgs.Handled)
                        {
                            SDL.SetWindowInputFocus(window).SdlThrowIf();
                        }
                    }
                    break;

                case SDLWindowEventID.HitTest:
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
        internal void ProcessInputKeyboard(SDLKeyboardEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            KeyState state = (KeyState)evnt.State;
            Key keyCode = (Key)SDL.GetKeyFromScancode(evnt.Keysym.Scancode);
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
        internal void ProcessInputText(SDLTextInputEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            keyboardCharEventArgs.Timestamp = evnt.Timestamp;
            keyboardCharEventArgs.Handled = false;
            keyboardCharEventArgs.Char = (char)evnt.Text_0;
            OnKeyboardCharInput(keyboardCharEventArgs);
        }

        /// <summary>
        /// Processes a mouse button event received from the message loop.
        /// </summary>
        /// <param name="evnt">The mouse button event to process.</param>
        internal void ProcessInputMouse(SDLMouseButtonEvent evnt)
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
        internal void ProcessInputMouse(SDLMouseMotionEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            if (lockCursor)
            {
                SDL.WarpMouseInWindow(window, 0, 0);
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
        internal void ProcessInputMouse(SDLMouseWheelEvent evnt)
        {
            Logger.ThrowIf(destroyed, "The window is already destroyed");
            mouseWheelEventArgs.Timestamp = evnt.Timestamp;
            mouseWheelEventArgs.Handled = false;
            mouseWheelEventArgs.MouseId = evnt.Which;
            mouseWheelEventArgs.Wheel = new(evnt.X, evnt.Y);
            mouseWheelEventArgs.Direction = (MouseWheelDirection)evnt.Direction;
            OnMouseWheelInput(mouseWheelEventArgs);
        }

        internal void ProcessInputTouchMotion(SDLTouchFingerEvent evnt)
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

        internal void ProcessInputTouchUp(SDLTouchFingerEvent evnt)
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

        internal void ProcessInputTouchDown(SDLTouchFingerEvent evnt)
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

        internal void DestroyWindow(bool appClose = false)
        {
            if (!appClose && Application.MainWindow == this)
            {
                return;
            }

            if (titlebar != null)
            {
                DetatchTitlebar(titlebar);
            }

            if (cursors != null)
            {
                for (SDLSystemCursor i = 0; i < SDLSystemCursor.NumSystemCursors; i++)
                {
                    SDL.FreeCursor(cursors[(int)i]);
                }
                Free(cursors);
                cursors = null;
            }

            if (window != null)
            {
                SDL.DestroyWindow(window);
                SdlCheckError();
                window = null;
            }

            destroyed = true;
            created = false;
        }

        internal void DestroyGraphics()
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
        }
    }
}