namespace Kitty
{
    using Kitty.Audio;
    using Kitty.D3D11;
    using Kitty.Debugging;
    using Kitty.Graphics;
    using Kitty.Input;
    using Kitty.OpenAL;
    using Kitty.Windows;
    using Kitty.Windows.Events;
    using Silk.NET.SDL;
    using System.Collections.Generic;
    using static Extensions.SdlErrorHandlingExtensions;

    public static unsafe class Application
    {
        internal static readonly Sdl sdl = Sdl.GetApi();

        private static bool initialized = false;
        private static bool exiting = false;
        private static readonly Dictionary<uint, IRenderWindow> windowIdToWindow = new();
        private static readonly List<IRenderWindow> windows = new();
        private static readonly List<Func<Event, bool>> hooks = new();

#nullable disable
        private static IRenderWindow mainWindow;
        private static AppBuilder builder;
        private static IGraphicsAdapter graphicsAdapter;
        private static IGraphicsDevice graphicsDevice;
        private static IGraphicsContext graphicsContext;
        private static IAudioDevice audioDevice;
#nullable restore

        /// <summary>
        /// Gets the main window of the application.
        /// </summary>
        public static IRenderWindow MainWindow => mainWindow;

        /// <summary>
        /// Gets the graphics device used by the application.
        /// </summary>
        public static IGraphicsDevice GraphicsDevice => graphicsDevice;

        /// <summary>
        /// Gets the graphics context used by the application.
        /// </summary>
        public static IGraphicsContext GraphicsContext => graphicsContext;

        /// <summary>
        /// Gets the graphics backend, eg. D3D11, D3D11On12, D3D12, OpenGL, Vulkan, Metal.
        /// </summary>
        public static GraphicsBackend Backend => GraphicsAdapter.Backend;

        public static bool IsD3D11()
        {
            return Backend == GraphicsBackend.D3D11 || Backend == GraphicsBackend.D3D11On12;
        }

        public static bool IsD3D11On12()
        {
            return Backend == GraphicsBackend.D3D11On12;
        }

        public static bool IsD3D12()
        {
            return Backend == GraphicsBackend.D3D12;
        }

        public static bool IsOpenGL()
        {
            return Backend == GraphicsBackend.OpenGL;
        }

        public static bool IsVulkan()
        {
            return Backend == GraphicsBackend.Vulkan;
        }

        public static bool IsMetal()
        {
            return Backend == GraphicsBackend.Metal;
        }

        public static bool GraphicsDebugging { get; set; }

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
            mainWindow.Closing += MainWindow_Closing;

            mainWindow.Show();
            PlatformRun();
        }

        private static void Init(IRenderWindow mainWindow, AppBuilder builder)
        {
            CrashLogger.Initialize();
            DXGIAdapterD3D11.Init(mainWindow, GraphicsDebugging);
            OpenALAdapter.Init();

#if DEBUG
            GraphicsDebugging = true;
#endif

            sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
            sdl.SetHint(Sdl.HintAutoUpdateJoysticks, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4Rumble, "1");
            sdl.SetHint(Sdl.HintJoystickRawinput, "0");
            sdl.Init(Sdl.InitEvents + Sdl.InitGamecontroller + Sdl.InitHaptic + Sdl.InitJoystick + Sdl.InitSensor);

            Keyboard.Init();
            Mouse.Init();
            Gamepads.Init();
            TouchDevices.Init();

            graphicsAdapter = GraphicsAdapter.ChooseAdapter(GraphicsBackend.Auto);
            graphicsDevice = graphicsAdapter.CreateGraphicsDevice(GraphicsDebugging);
            graphicsContext = graphicsDevice.Context;
            audioDevice = AudioAdapter.CreateAudioDevice(AudioBackend.Auto, null);

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Initialize(builder, audioDevice, graphicsDevice);
            }

            initialized = true;
        }

        internal static void RegisterWindow(IRenderWindow window)
        {
            windows.Add(window);
            windowIdToWindow.Add(window.WindowID, window);
            if (initialized)
                window.Initialize(builder, audioDevice, graphicsDevice);
        }

        private static void MainWindow_Closing(object? sender, CloseEventArgs e)
        {
            if (!e.Handled)
                exiting = true;
        }

        public static void RegisterHook(Func<Event, bool> hook)
        {
            hooks.Add(hook);
        }

        private static void PlatformRun()
        {
            Event evnt;
            Time.Initialize();

            while (!exiting)
            {
                sdl.PumpEvents();
                while (sdl.PollEvent(&evnt) == (int)SdlBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }
                    EventType type = (EventType)evnt.Type;

                    switch (type)
                    {
                        case EventType.Firstevent:
                            break;

                        case EventType.Quit:
                            exiting = true;
                            break;

                        case EventType.AppTerminating:
                            exiting = true;
                            break;

                        case EventType.AppLowmemory:
                            break;

                        case EventType.AppWillenterbackground:
                            break;

                        case EventType.AppDidenterbackground:
                            break;

                        case EventType.AppWillenterforeground:
                            break;

                        case EventType.AppDidenterforeground:
                            break;

                        case EventType.Localechanged:
                            break;

                        case EventType.Displayevent:
                            break;

                        case EventType.Windowevent:
                            {
                                var even = evnt.Window;
                                if (even.WindowID == mainWindow.WindowID)
                                {
                                    ((SdlWindow)mainWindow).ProcessEvent(even);
                                    if ((WindowEventID)evnt.Window.Event == WindowEventID.Close)
                                    {
                                        exiting = true;
                                    }
                                }
                            }

                            break;

                        case EventType.Syswmevent:
                            break;

                        case EventType.Keydown:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputKeyboard(even);
                            }
                            break;

                        case EventType.Keyup:
                            {
                                var even = evnt.Key;
                                Keyboard.OnKeyUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputKeyboard(even);
                            }
                            break;

                        case EventType.Textediting:
                            break;

                        case EventType.Textinput:
                            {
                                var even = evnt.Text;
                                Keyboard.OnTextInput(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputText(even);
                            }
                            break;

                        case EventType.Keymapchanged:
                            break;

                        case EventType.Mousemotion:
                            {
                                var even = evnt.Motion;
                                Mouse.OnMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousebuttondown:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousebuttonup:
                            {
                                var even = evnt.Button;
                                Mouse.OnButtonUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Mousewheel:
                            {
                                var even = evnt.Wheel;
                                Mouse.OnWheel(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputMouse(even);
                            }
                            break;

                        case EventType.Joyaxismotion:
                            {
                                var even = evnt.Jaxis;
                                Joysticks.OnAxisMotion(even);
                            }
                            break;

                        case EventType.Joyballmotion:
                            {
                                var even = evnt.Jball;
                                Joysticks.OnBallMotion(even);
                            }
                            break;

                        case EventType.Joyhatmotion:
                            {
                                var even = evnt.Jhat;
                                Joysticks.OnHatMotion(even);
                            }
                            break;

                        case EventType.Joybuttondown:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonDown(even);
                            }
                            break;

                        case EventType.Joybuttonup:
                            {
                                var even = evnt.Jbutton;
                                Joysticks.OnButtonUp(even);
                            }
                            break;

                        case EventType.Joydeviceadded:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.AddJoystick(even);
                            }
                            break;

                        case EventType.Joydeviceremoved:
                            {
                                var even = evnt.Jdevice;
                                Joysticks.RemoveJoystick(even);
                            }
                            break;

                        case EventType.Controlleraxismotion:
                            {
                                var even = evnt.Caxis;
                                Gamepads.OnAxisMotion(even);
                            }
                            break;

                        case EventType.Controllerbuttondown:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.OnButtonDown(even);
                            }
                            break;

                        case EventType.Controllerbuttonup:
                            {
                                var even = evnt.Cbutton;
                                Gamepads.OnButtonUp(even);
                            }
                            break;

                        case EventType.Controllerdeviceadded:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.AddController(even);
                            }
                            break;

                        case EventType.Controllerdeviceremoved:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.RemoveController(even);
                            }
                            break;

                        case EventType.Controllerdeviceremapped:
                            {
                                var even = evnt.Cdevice;
                                Gamepads.OnRemapped(even);
                            }
                            break;

                        case EventType.Controllertouchpaddown:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.OnTouchPadDown(even);
                            }
                            break;

                        case EventType.Controllertouchpadmotion:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.OnTouchPadMotion(even);
                            }
                            break;

                        case EventType.Controllertouchpadup:
                            {
                                var even = evnt.Ctouchpad;
                                Gamepads.OnTouchPadUp(even);
                            }
                            break;

                        case EventType.Controllersensorupdate:
                            {
                                var even = evnt.Csensor;
                                Gamepads.OnSensorUpdate(even);
                            }
                            break;

                        case EventType.Fingerdown:
                            {
                                /*
                                var even = evnt.Tfinger;
                                TouchDevices.FingerDown(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchDown(even);
                                */
                            }
                            break;

                        case EventType.Fingerup:
                            {
                                var even = evnt.Tfinger;
                                TouchDevices.FingerUp(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchUp(even);
                            }
                            break;

                        case EventType.Fingermotion:
                            {
                                var even = evnt.Tfinger;
                                TouchDevices.FingerMotion(even);
                                if (even.WindowID == mainWindow.WindowID)
                                    ((SdlWindow)mainWindow).ProcessInputTouchMotion(even);
                            }
                            break;

                        case EventType.Dollargesture:
                            break;

                        case EventType.Dollarrecord:
                            break;

                        case EventType.Multigesture:
                            break;

                        case EventType.Clipboardupdate:
                            break;

                        case EventType.Dropfile:
                            break;

                        case EventType.Droptext:
                            break;

                        case EventType.Dropbegin:
                            break;

                        case EventType.Dropcomplete:
                            break;

                        case EventType.Audiodeviceadded:
                            break;

                        case EventType.Audiodeviceremoved:
                            break;

                        case EventType.Sensorupdate:
                            break;

                        case EventType.RenderTargetsReset:
                            break;

                        case EventType.RenderDeviceReset:
                            break;

                        case EventType.Userevent:
                            break;

                        case EventType.Lastevent:
                            break;
                    }
                }

                mainWindow.Render(graphicsContext);
                mainWindow.ClearState();
                graphicsAdapter.PumpDebugMessages();
                Time.FrameUpdate();
            }

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].Uninitialize();
            }

             ((SdlWindow)mainWindow).DestroyWindow();

            SdlCheckError();
            sdl.Quit();
        }
    }
}