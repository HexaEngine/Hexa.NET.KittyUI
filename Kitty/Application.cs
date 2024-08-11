namespace Hexa.NET.Kitty
{
    using Hexa.NET.Kitty.Audio;
    using Hexa.NET.Kitty.Debugging;
    using Hexa.NET.Kitty.Input;
    using Hexa.NET.Kitty.OpenAL;
    using Hexa.NET.Kitty.Windows;
    using Hexa.NET.Kitty.Windows.Events;
    using Hexa.NET.SDL2;
    using System.Collections.Generic;
    using static Extensions.SdlErrorHandlingExtensions;

    public delegate bool EventHook(SDLEvent evnt);

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

        public static GraphicsBackend GraphicsBackend => ((SdlWindow)mainWindow).Backend;

        public static void Run()
        {
            Run(new Windows.Window(), new AppBuilder());
        }

        public static void Run(AppBuilder builder)
        {
            Run(new Windows.Window(), builder);
        }

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
            CrashLogger.Initialize();
            OpenALAdapter.Init();

#if DEBUG
            GraphicsDebugging = true;
#endif

            SDL.SDLSetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SDLSetHint(SDL.SDL_HINT_AUTO_UPDATE_JOYSTICKS, "1");
            SDL.SDLSetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1");//HintJoystickHidapiPS4
            SDL.SDLSetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1"); //HintJoystickHidapiPS4Rumble
            SDL.SDLSetHint(SDL.SDL_HINT_JOYSTICK_RAWINPUT, "0");
            SDL.SDLSetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1"); //HintWindowsDisableThreadNaming
            SDL.SDLSetHint(SDL.SDL_HINT_MOUSE_NORMAL_SPEED_SCALE, "1");

            SDL.SDLInit(SDL.SDL_INIT_EVENTS + SDL.SDL_INIT_GAMECONTROLLER + SDL.SDL_INIT_HAPTIC + SDL.SDL_INIT_JOYSTICK + SDL.SDL_INIT_SENSOR);

            Keyboard.Init();
            Mouse.Init();
            Gamepads.Init();
            TouchDevices.Init();

            audioDevice = AudioAdapter.CreateAudioDevice(AudioBackend.Auto, null);

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Initialize(builder, audioDevice);
            }

            initialized = true;
        }

        internal static void RegisterWindow(IRenderWindow window)
        {
            windows.Add(window);
            windowIdToWindow.Add(window.WindowID, window);
            if (initialized)
                window.Initialize(builder, audioDevice);
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

        private static void PlatformRun()
        {
            SDLEvent evnt;
            Time.Initialize();

            while (!exiting)
            {
                SDL.SDLPumpEvents();
                while (SDL.SDLPollEvent(&evnt) == (int)SDLBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }
                    SDLEventType type = (SDLEventType)evnt.Type;

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
                                    ((SdlWindow)mainWindow).ProcessEvent(even);
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
                                    ((SdlWindow)mainWindow).ProcessInputKeyboard(even);
                            }
                            break;

                        case SDLEventType.Keyup:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputKeyboard(even);
                            }
                            break;

                        case SDLEventType.Textediting:
                            break;

                        case SDLEventType.Textinput:
                            {
                                var even = evnt.Text;
                                Keyboard.OnTextInput(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputText(even);
                            }
                            break;

                        case SDLEventType.Keymapchanged:
                            break;

                        case SDLEventType.Mousemotion:
                            {
                                var even = evnt.Motion;
                                Mouse.OnMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousebuttondown:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousebuttonup:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case SDLEventType.Mousewheel:
                            {
                                var even = evnt.Wheel;
                                Mouse.OnWheel(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
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
                                /*
                                var even = evnt.Tfinger;
                                TouchDevices.FingerDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchDown(even);
                                */
                            }
                            break;

                        case SDLEventType.Fingerup:
                            {
                                var even = evnt.Tfinger;
                                TouchDevices.FingerUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchUp(even);
                            }
                            break;

                        case SDLEventType.Fingermotion:
                            {
                                var even = evnt.Tfinger;
                                TouchDevices.FingerMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchMotion(even);
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
                            break;

                        case SDLEventType.Droptext:
                            break;

                        case SDLEventType.Dropbegin:
                            break;

                        case SDLEventType.Dropcomplete:
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

                mainWindow.Render();
                mainWindow.ClearState();

                Time.FrameUpdate();
            }

            ((SdlWindow)mainWindow).DestroyWindow(true);

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Uninitialize();
            }

            ((SdlWindow)mainWindow).DestroyGraphics();

            SdlCheckError();
            SDL.SDLQuit();
        }
    }
}