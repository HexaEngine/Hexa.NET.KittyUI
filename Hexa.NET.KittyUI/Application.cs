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
    using Hexa.NET.SDL2;
    using System.Collections.Generic;
    using static Hexa.NET.KittyUI.Extensions.SdlErrorHandlingExtensions;

    public static unsafe class Application
    {
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

        public static void Run(IRenderWindow mainWindow, AppBuilder builder)
        {
            Init(mainWindow, builder);

            Application.mainWindow = mainWindow;
            Application.builder = builder;
            mainWindow.Closed += MainWindowClosed;
            builder.Dispose();
            mainWindow.Show();
            PlatformRun();
        }

        private static void Init(IRenderWindow mainWindow, AppBuilder builder)
        {
            if (LoggingEnabled)
            {
                CrashLogger.Initialize();
            }

            if (ImGuiDebugTools.Enabled)
            {
                ImGuiDebugTools.Init();
            }

            OpenALAdapter.Init();

#if DEBUG
            GraphicsDebugging = true;
#endif

            SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SetHint(SDL.SDL_HINT_AUTO_UPDATE_JOYSTICKS, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1"); // HintJoystickHidapiPS4
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1"); // HintJoystickHidapiPS4Rumble
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_RAWINPUT, "0");
            SDL.SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1"); // HintWindowsDisableThreadNaming
            SDL.SetHint(SDL.SDL_HINT_MOUSE_NORMAL_SPEED_SCALE, "1");
            SDL.SetHint(SDL.SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
            SDL.SetHint(SDL.SDL_HINT_IME_SHOW_UI, "1");

            SDL.Init(SDL.SDL_INIT_EVENTS + SDL.SDL_INIT_GAMECONTROLLER + SDL.SDL_INIT_HAPTIC + SDL.SDL_INIT_JOYSTICK + SDL.SDL_INIT_SENSOR);

            SdlCheckError();

            Keyboard.Init();
            Mouse.Init();
            Gamepads.Init();
            TouchDevices.Init();

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
                while (SDL.PollEvent(&evnt) == (int)SDLBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        if (hooks[i](evnt))
                        {
                            break;
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

            ((CoreWindow)mainWindow).DestroyWindow(true);

            ((CoreWindow)mainWindow).DestroyGraphics();
            
            AudioManager.Dispose();

            SDL.Quit();
        }

        private static void HandleEvent(SDLEvent evnt, SDLEventType type)
        {
            switch (type)
            {
                case SDLEventType.Firstevent:
                    break;

                case SDLEventType.Quit:
                    if (!supressQuitApp)
                    {
                        exiting = true;
                    }
                    supressQuitApp = false;
                    break;

                case SDLEventType.AppTerminating:
                    exiting = true;
                    break;

                case SDLEventType.AppLowmemory:
                    break;

                case SDLEventType.AppWillenterbackground:
                    break;

                case SDLEventType.AppDidenterbackground:
                    break;

                case SDLEventType.AppWillenterforeground:
                    break;

                case SDLEventType.AppDidenterforeground:
                    break;

                case SDLEventType.Localechanged:
                    break;

                case SDLEventType.Displayevent:
                    break;

                case SDLEventType.Windowevent:
                    {
                        var even = evnt.Window;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            ((CoreWindow)mainWindow).ProcessEvent(even);
                        }
                    }

                    break;

                case SDLEventType.Syswmevent:
                    break;

                case SDLEventType.Keydown:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputKeyboard(even);
                    }
                    break;

                case SDLEventType.Keyup:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputKeyboard(even);
                    }
                    break;

                case SDLEventType.Textediting:
                    break;

                case SDLEventType.Textinput:
                    {
                        var even = evnt.Text;
                        Keyboard.OnTextInput(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputText(even);
                    }
                    break;

                case SDLEventType.Keymapchanged:
                    break;

                case SDLEventType.Mousemotion:
                    {
                        var even = evnt.Motion;
                        Mouse.OnMotion(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.Mousebuttondown:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.Mousebuttonup:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.Mousewheel:
                    {
                        var even = evnt.Wheel;
                        Mouse.OnWheel(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputMouse(even);
                    }
                    break;

                case SDLEventType.Joyaxismotion:
                    {
                        var even = evnt.Jaxis;
                        Joysticks.OnAxisMotion(even);
                    }
                    break;

                case SDLEventType.Joyballmotion:
                    {
                        var even = evnt.Jball;
                        Joysticks.OnBallMotion(even);
                    }
                    break;

                case SDLEventType.Joyhatmotion:
                    {
                        var even = evnt.Jhat;
                        Joysticks.OnHatMotion(even);
                    }
                    break;

                case SDLEventType.Joybuttondown:
                    {
                        var even = evnt.Jbutton;
                        Joysticks.OnButtonDown(even);
                    }
                    break;

                case SDLEventType.Joybuttonup:
                    {
                        var even = evnt.Jbutton;
                        Joysticks.OnButtonUp(even);
                    }
                    break;

                case SDLEventType.Joydeviceadded:
                    {
                        var even = evnt.Jdevice;
                        Joysticks.AddJoystick(even);
                    }
                    break;

                case SDLEventType.Joydeviceremoved:
                    {
                        var even = evnt.Jdevice;
                        Joysticks.RemoveJoystick(even);
                    }
                    break;

                case SDLEventType.Controlleraxismotion:
                    {
                        var even = evnt.Caxis;
                        Gamepads.OnAxisMotion(even);
                    }
                    break;

                case SDLEventType.Controllerbuttondown:
                    {
                        var even = evnt.Cbutton;
                        Gamepads.OnButtonDown(even);
                    }
                    break;

                case SDLEventType.Controllerbuttonup:
                    {
                        var even = evnt.Cbutton;
                        Gamepads.OnButtonUp(even);
                    }
                    break;

                case SDLEventType.Controllerdeviceadded:
                    {
                        var even = evnt.Cdevice;
                        Gamepads.AddController(even);
                    }
                    break;

                case SDLEventType.Controllerdeviceremoved:
                    {
                        var even = evnt.Cdevice;
                        Gamepads.RemoveController(even);
                    }
                    break;

                case SDLEventType.Controllerdeviceremapped:
                    {
                        var even = evnt.Cdevice;
                        Gamepads.OnRemapped(even);
                    }
                    break;

                case SDLEventType.Controllertouchpaddown:
                    {
                        var even = evnt.Ctouchpad;
                        Gamepads.OnTouchPadDown(even);
                    }
                    break;

                case SDLEventType.Controllertouchpadmotion:
                    {
                        var even = evnt.Ctouchpad;
                        Gamepads.OnTouchPadMotion(even);
                    }
                    break;

                case SDLEventType.Controllertouchpadup:
                    {
                        var even = evnt.Ctouchpad;
                        Gamepads.OnTouchPadUp(even);
                    }
                    break;

                case SDLEventType.Controllersensorupdate:
                    {
                        var even = evnt.Csensor;
                        Gamepads.OnSensorUpdate(even);
                    }
                    break;

                case SDLEventType.Fingerdown:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerDown(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchDown(even);
                    }
                    break;

                case SDLEventType.Fingerup:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerUp(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchUp(even);
                    }
                    break;

                case SDLEventType.Fingermotion:
                    {
                        var even = evnt.Tfinger;
                        TouchDevices.FingerMotion(even);
                        if (even.WindowID == mainWindow.WindowID)
                            ((CoreWindow)mainWindow).ProcessInputTouchMotion(even);
                    }
                    break;

                case SDLEventType.Dollargesture:
                    break;

                case SDLEventType.Dollarrecord:
                    break;

                case SDLEventType.Multigesture:
                    break;

                case SDLEventType.Clipboardupdate:
                    break;

                case SDLEventType.Dropfile:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropFile(even);
                        }

                        SDL.Free(evnt.Drop.File);
                    }
                    break;

                case SDLEventType.Droptext:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropText(even);
                        }

                        SDL.Free(evnt.Drop.File);
                    }
                    break;

                case SDLEventType.Dropbegin:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropBegin(even);
                        }
                    }
                    break;

                case SDLEventType.Dropcomplete:
                    {
                        var even = evnt.Drop;
                        if (even.WindowID == mainWindow.WindowID)
                        {
                            //((SdlWindow)mainWindow).ProcessDropComplete(even);
                        }
                    }
                    break;

                case SDLEventType.Audiodeviceadded:
                    break;

                case SDLEventType.Audiodeviceremoved:
                    break;

                case SDLEventType.Sensorupdate:
                    break;

                case SDLEventType.RenderTargetsReset:
                    break;

                case SDLEventType.RenderDeviceReset:
                    break;

                case SDLEventType.Userevent:
                    break;

                case SDLEventType.Lastevent:
                    break;
            }
        }
    }
}