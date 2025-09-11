namespace Hexa.NET.KittyUI
{
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.Input;
    using Hexa.NET.KittyUI.OpenAL;
    using Hexa.NET.KittyUI.Windows;
    using Hexa.NET.KittyUI.Windows.Events;
    using Hexa.NET.Logging;
    using Hexa.NET.SDL3;
    using System.Collections.Generic;
    using static Hexa.NET.KittyUI.Extensions.SdlErrorHandlingExtensions;

    public static unsafe class Application
    {
        private static bool earlyInitialized = false;
        private static bool initialized = false;
        private static bool exiting = false;
        private static readonly Dictionary<uint, IRenderWindow> windowIdToWindow = new();
        private static readonly List<IRenderWindow> windows = new();
        private static readonly List<EventHook> hooks = new();

#nullable disable
        private static IRenderWindow mainWindow;
        private static AppBuilder builder;

        private static IAudioDevice audioDevice;
        private static bool supressQuitApp;
#nullable restore

        /// <summary>
        /// Gets the main window of the application.
        /// </summary>
        public static IRenderWindow MainWindow => mainWindow;

        public static bool GraphicsDebugging { get; set; }

        public static GraphicsBackend GraphicsBackend => ((CoreWindow)mainWindow).Backend;

        /// <summary>
        /// Sets the graphics backend. Default: <see cref="GraphicsBackend.Auto"/>
        /// </summary>
        /// <remarks>This can be only set BEFORE <see cref="Run"/>  <see cref="AppBuilder.Run"/>.</remarks>
        public static GraphicsBackend SelectedGraphicsBackend { get; set; } = GraphicsBackend.Auto;

        /// <summary>
        /// Sets the audio backend. Default: <see cref="AudioBackend.Auto"/>
        /// </summary>
        /// <remarks>This can be only set BEFORE <see cref="Run"/>  <see cref="AppBuilder.Run"/>.</remarks>
        public static AudioBackend SelectedAudioBackend { get; set; } = AudioBackend.Auto;

        /// <summary>
        /// Sets the active sub systems. Default: <see cref="SubSystems.None"/>
        /// </summary>
        /// <remarks>This can be only set BEFORE <see cref="Run"/>  <see cref="AppBuilder.Run"/>.</remarks>
        public static SubSystems SubSystems { get; set; } = SubSystems.None;

        public static bool LoggingEnabled { get; set; } = true;

        public static event Action? Exiting;

        public static void Run(IRenderWindow mainWindow, AppBuilder builder)
        {
            Init(mainWindow, builder);

            Application.mainWindow = mainWindow;
            Application.builder = builder;
            mainWindow.Closed += MainWindowClosed;
            builder.Dispose();
            mainWindow.Show();
            PlatformRun();

            FileLogWriter?.Dispose();
        }

        public static LogFileWriter? FileLogWriter { get; set; }

        public static SDLInitFlags InitFlags { get; set; } = SDLInitFlags.Events | SDLInitFlags.Video | SDLInitFlags.Joystick | SDLInitFlags.Gamepad;
        
        internal static void EarlyInit()
        {
            if (earlyInitialized) return;

            if (LoggingEnabled)
            {
                FileLogWriter = new("logs");
                CrashLogger.Initialize();
                LoggerFactory.AddGlobalWriter(FileLogWriter);
            }

            SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SetHint(SDL.SDL_HINT_AUTO_UPDATE_JOYSTICKS, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1"); // HintJoystickHidapiPS4
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_RAWINPUT, "0");
            SDL.SetHint(SDL.SDL_HINT_MOUSE_NORMAL_SPEED_SCALE, "1");
            SDL.SetHint(SDL.SDL_HINT_MOUSE_AUTO_CAPTURE, "0");

            SDL.Init(InitFlags);

            SdlCheckError();

            Keyboard.Init();
            Mouse.Init();
            Gamepads.Init();
            TouchDevices.Init();

            earlyInitialized = true;
        }

        private static void Init(IRenderWindow mainWindow, AppBuilder builder)
        {
            if (ImGuiDebugTools.Enabled)
            {
                ImGuiDebugTools.Init();
            }

            OpenALAdapter.Init();

#if DEBUG
            GraphicsDebugging = true;
#endif

            if ((SubSystems & SubSystems.Audio) != 0)
            {
                audioDevice = AudioAdapter.CreateAudioDevice(SelectedAudioBackend, null);
                AudioManager.Initialize(audioDevice);
            }

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Initialize(builder);
            }

            initialized = true;
        }

        /// <summary>
        /// For Lazy init.
        /// </summary>
        /// <param name="subSystem"></param>
        public static void InitSubSystem(SubSystems subSystem)
        {
            if ((SubSystems & SubSystems.Audio) != 0)
            {
                audioDevice = AudioAdapter.CreateAudioDevice(SelectedAudioBackend, null);
                AudioManager.Initialize(audioDevice);
            }

            SubSystems |= subSystem;
        }

        /// <summary>
        /// For Lazy shutdown.
        /// </summary>
        /// <param name="subSystem"></param>
        public static void ShutdownSubSystem(SubSystems subSystem)
        {
            if ((SubSystems & SubSystems.Audio) != 0)
            {
                AudioManager.Dispose();
            }

            SubSystems &= ~subSystem;
        }

        internal static void RegisterWindow(IRenderWindow window)
        {
            windows.Add(window);
            windowIdToWindow.Add(window.WindowID, window);
            if (initialized)
                window.Initialize(builder);
        }

        /// <summary>
        /// Suppresses the quit application action. Will be automatically reset.
        /// </summary>
        internal static void SuppressQuitApp()
        {
            supressQuitApp = true;
        }

        private static void MainWindowClosed(object? sender, CloseEventArgs e)
        {
            exiting = true;
        }

        public static void RegisterHook(EventHook hook)
        {
            hooks.Add(hook);
        }

        public static bool UnregisterHook(EventHook hook)
        {
            return hooks.Remove(hook);
        }

        public static void Exit()
        {
            if (supressQuitApp)
            {
                supressQuitApp = false;
                return;
            }

            exiting = true;
        }

        private static void PlatformRun()
        {
            SDLEvent evnt;
            Time.Initialize();

            while (!exiting)
            {
                SDL.PumpEvents();
                while (SDL.PollEvent(&evnt))
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        if (hooks[i](evnt))
                        {
                        }
                    }
                    SDLEventType type = (SDLEventType)evnt.Type;
                    HandleEvent(evnt, type);
                }

                mainWindow.Render();
                mainWindow.ClearState();

                Time.FrameUpdate();
            }

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Uninitialize();
            }

            Exiting?.Invoke();

            ((CoreWindow)mainWindow).DestroyWindow(true);

            AudioManager.Dispose();

            SDL.Quit();
        }

        private static void HandleEvent(SDLEvent evnt, SDLEventType type)
        {
            switch (type)
            {
                case SDLEventType.Quit:
                    if (!supressQuitApp)
                    {
                        exiting = true;
                    }
                    supressQuitApp = false;
                    break;

                case SDLEventType.Terminating:
                    exiting = true;
                    break;

                case SDLEventType.LowMemory:
                    break;

                case SDLEventType.WillEnterBackground:
                    break;

                case SDLEventType.DidEnterBackground:
                    break;

                case SDLEventType.WillEnterForeground:
                    break;

                case SDLEventType.DidEnterForeground:
                    break;

                case SDLEventType.LocaleChanged:
                    break;

                case >= SDLEventType.WindowFirst and <= SDLEventType.WindowLast:
                    {
                        var even = evnt.Window;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            ((CoreWindow)mainWindow).ProcessEvent(even);
                        }
                    }

                    break;


                case SDLEventType.KeyDown:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputKeyboard(even);
                    }
                    break;

                case SDLEventType.KeyUp:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputKeyboard(even);
                    }
                    break;

                case SDLEventType.TextEditing:
                    break;

                case SDLEventType.TextInput:
                    {
                        var even = evnt.Text;
                        Keyboard.OnTextInput(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputText(even);
                    }
                    break;

                case SDLEventType.KeymapChanged:
                    break;

                case SDLEventType.MouseMotion:
                    {
                        var even = evnt.Motion;
                        Mouse.OnMotion(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.MouseButtonDown:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.MouseButtonUp:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.MouseWheel:
                    {
                        var even = evnt.Wheel;
                        Mouse.OnWheel(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.JoystickAxisMotion:
                    {
                        var even = evnt.Jaxis;
                        Joysticks.OnAxisMotion(even);
                    }
                    break;

                case SDLEventType.JoystickBallMotion:
                    {
                        var even = evnt.Jball;
                        Joysticks.OnBallMotion(even);
                    }
                    break;

                case SDLEventType.JoystickHatMotion:
                    {
                        var even = evnt.Jhat;
                        Joysticks.OnHatMotion(even);
                    }
                    break;

                case SDLEventType.JoystickButtonDown:
                    {
                        var even = evnt.Jbutton;
                        Joysticks.OnButtonDown(even);
                    }
                    break;

                case SDLEventType.JoystickButtonUp:
                    {
                        var even = evnt.Jbutton;
                        Joysticks.OnButtonUp(even);
                    }
                    break;

                case SDLEventType.JoystickAdded:
                    {
                        var even = evnt.Jdevice;
                        Joysticks.AddJoystick(even);
                    }
                    break;

                case SDLEventType.JoystickRemoved:
                    {
                        var even = evnt.Jdevice;
                        Joysticks.RemoveJoystick(even);
                    }
                    break;

                case SDLEventType.GamepadAxisMotion:
                    {
                        var even = evnt.Gaxis;
                        Gamepads.OnAxisMotion(even);
                    }
                    break;

                case SDLEventType.GamepadButtonDown:
                    {
                        var even = evnt.Gbutton;
                        Gamepads.OnButtonDown(even);
                    }
                    break;

                case SDLEventType.GamepadButtonUp:
                    {
                        var even = evnt.Gbutton;
                        Gamepads.OnButtonUp(even);
                    }
                    break;

                case SDLEventType.GamepadAdded:
                    {
                        var even = evnt.Gdevice;
                        Gamepads.AddController(even);
                    }
                    break;

                case SDLEventType.GamepadRemoved:
                    {
                        var even = evnt.Gdevice;
                        Gamepads.RemoveController(even);
                    }
                    break;

                case SDLEventType.GamepadRemapped:
                    {
                        var even = evnt.Gdevice;
                        Gamepads.OnRemapped(even);
                    }
                    break;

                case SDLEventType.GamepadTouchpadDown:
                    {
                        var even = evnt.Gtouchpad;
                        Gamepads.OnTouchPadDown(even);
                    }
                    break;

                case SDLEventType.GamepadTouchpadMotion:
                    {
                        var even = evnt.Gtouchpad;
                        Gamepads.OnTouchPadMotion(even);
                    }
                    break;

                case SDLEventType.GamepadTouchpadUp:
                    {
                        var even = evnt.Gtouchpad;
                        Gamepads.OnTouchPadUp(even);
                    }
                    break;

                case SDLEventType.GamepadSensorUpdate:
                    {
                        var even = evnt.Gsensor;
                        Gamepads.OnSensorUpdate(even);
                    }
                    break;

                case SDLEventType.FingerDown:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchDown(even);
                    }
                    break;

                case SDLEventType.FingerUp:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchUp(even);
                    }
                    break;

                case SDLEventType.FingerMotion:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerMotion(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchMotion(even);
                    }
                    break;

              
                case SDLEventType.ClipboardUpdate:
                    break;

                case SDLEventType.DropFile:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropFile(even);
                        }
                    }
                    break;

                case SDLEventType.DropText:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropText(even);
                        }
                    }
                    break;

                case SDLEventType.DropBegin:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropBegin(even);
                        }
                    }
                    break;

                case SDLEventType.DropComplete:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropComplete(even);
                        }
                    }
                    break;

                case SDLEventType.AudioDeviceAdded:
                    break;

                case SDLEventType.AudioDeviceRemoved:
                    break;

                case SDLEventType.SensorUpdate:
                    break;

                case SDLEventType.RenderTargetsReset:
                    break;

                case SDLEventType.RenderDeviceReset:
                    break;

                case SDLEventType.User:
                    break;
            }
        }

       
    }
}