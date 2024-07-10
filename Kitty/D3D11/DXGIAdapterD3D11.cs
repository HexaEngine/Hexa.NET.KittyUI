﻿namespace Kitty.D3D11
{
    using Kitty;
    using Kitty.Graphics;
    using Kitty.Logging;
    using Kitty.Windows;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Silk.NET.Maths;
    using Silk.NET.SDL;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using Window = Silk.NET.SDL.Window;
    using InfoQueueFilter = Silk.NET.DXGI.InfoQueueFilter;
    using System.Text;

    public unsafe class DXGIAdapterD3D11 : IGraphicsAdapter
    {
        internal readonly DXGI DXGI;
        protected readonly INativeWindowSource source;
        private readonly bool debug;

        internal ComPtr<IDXGIFactory7> IDXGIFactory;
        internal ComPtr<IDXGIAdapter4> IDXGIAdapter;
        internal ComPtr<IDXGIOutput6> IDXGIOutput;

        internal ComPtr<IDXGIDebug> IDXGIDebug;
        internal ComPtr<IDXGIInfoQueue> IDXGIInfoQueue;

        private readonly Guid DXGI_DEBUG_ALL = new(0xe48ae283, 0xda80, 0x490b, 0x87, 0xe6, 0x43, 0xe9, 0xa9, 0xcf, 0xda, 0x8);
        private readonly Guid DXGI_DEBUG_DX = new(0x35cdd7fc, 0x13b2, 0x421d, 0xa5, 0xd7, 0x7e, 0x44, 0x51, 0x28, 0x7d, 0x64);
        private readonly Guid DXGI_DEBUG_DXGI = new(0x25cddaa4, 0xb1c6, 0x47e1, 0xac, 0x3e, 0x98, 0x87, 0x5b, 0x5a, 0x2e, 0x2a);
        private readonly Guid DXGI_DEBUG_APP = new(0x6cd6e01, 0x4219, 0x4ebd, 0x87, 0x9, 0x27, 0xed, 0x23, 0x36, 0xc, 0x62);
        private readonly Guid DXGI_DEBUG_D3D11 = new(0x4b99317b, 0xac39, 0x4aa6, 0xbb, 0xb, 0xba, 0xa0, 0x47, 0x84, 0x79, 0x8f);

        public DXGIAdapterD3D11(INativeWindowSource source, bool debug)
        {
            DXGI = DXGI.GetApi(source);

            if (debug)
            {
                DXGI.GetDebugInterface1(0, out IDXGIDebug);
                DXGI.GetDebugInterface1(0, out IDXGIInfoQueue);

                InfoQueueFilter filter = new();
                filter.DenyList.NumIDs = 1;
                filter.DenyList.PIDList = (int*)AllocT(MessageID.SetprivatedataChangingparams);
                IDXGIInfoQueue.AddStorageFilterEntries(DXGI_DEBUG_ALL, &filter);
                IDXGIInfoQueue.SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Message, false);
                IDXGIInfoQueue.SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Info, false);
                IDXGIInfoQueue.SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Warning, true);
                IDXGIInfoQueue.SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Error, true);
                IDXGIInfoQueue.SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Corruption, true);
                Free(filter.DenyList.PIDList);
            }

            DXGI.CreateDXGIFactory2(debug ? 0x01u : 0x00u, out IDXGIFactory);

            IDXGIAdapter = GetHardwareAdapter(null);
            IDXGIOutput = GetOutput(null);
            this.source = source;
            this.debug = debug;
        }

        public static void Init(INativeWindowSource window, bool debug)
        {
            if (OperatingSystem.IsWindows())
            {
                GraphicsAdapter.Adapters.Add(new DXGIAdapterD3D11(window, debug));
            }
        }

        public static string Convert(InfoQueueMessageSeverity severity)
        {
            return severity switch
            {
                InfoQueueMessageSeverity.Corruption => "CORRUPTION",
                InfoQueueMessageSeverity.Error => "ERROR",
                InfoQueueMessageSeverity.Warning => "WARNING",
                InfoQueueMessageSeverity.Info => "INFO",
                InfoQueueMessageSeverity.Message => "LOG",
                _ => throw new NotImplementedException(),
            };
        }

        public static string Convert(InfoQueueMessageCategory category)
        {
            return category switch
            {
                InfoQueueMessageCategory.Unknown => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_UNKNOWN",
                InfoQueueMessageCategory.Miscellaneous => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_MISCELLANEOUS",
                InfoQueueMessageCategory.Initialization => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_INITIALIZATION",
                InfoQueueMessageCategory.Cleanup => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_CLEANUP",
                InfoQueueMessageCategory.Compilation => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_COMPILATION",
                InfoQueueMessageCategory.StateCreation => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_STATE_CREATION",
                InfoQueueMessageCategory.StateSetting => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_STATE_SETTING",
                InfoQueueMessageCategory.StateGetting => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_STATE_GETTING",
                InfoQueueMessageCategory.ResourceManipulation => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_RESOURCE_MANIPULATION",
                InfoQueueMessageCategory.Execution => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_EXECUTION",
                InfoQueueMessageCategory.Shader => "DXGI_INFO_QUEUE_MESSAGE_CATEGORY_SHADER",
                _ => throw new NotImplementedException(),
            };
        }

        public void PumpDebugMessages()
        {
            if (!debug)
                return;
            ulong messageCount = IDXGIInfoQueue.GetNumStoredMessages(DXGI_DEBUG_ALL);
            for (ulong i = 0; i < messageCount; i++)
            {
                nuint messageLength;

                HResult hr = IDXGIInfoQueue.GetMessageA(DXGI_DEBUG_ALL, i, (InfoQueueMessage*)null, &messageLength);

                if (hr.IsSuccess)
                {
                    InfoQueueMessage* message = (InfoQueueMessage*)Alloc(messageLength);

                    hr = IDXGIInfoQueue.GetMessageA(DXGI_DEBUG_ALL, i, message, &messageLength);

                    if (hr.IsSuccess)
                    {
                        string msg = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(message->PDescription));

                        if (message->Producer == DXGI_DEBUG_DX)
                            Logger.Log($"DX {Convert(message->Severity)}: {msg} [ {Convert(message->Category)} ]");
                        if (message->Producer == DXGI_DEBUG_DXGI)
                            Logger.Log($"DXGI {Convert(message->Severity)}: {msg} [ {Convert(message->Category)} ]");
                        if (message->Producer == DXGI_DEBUG_APP)
                            Logger.Log($"APP {Convert(message->Severity)}: {msg} [ {Convert(message->Category)} ]");
                        if (message->Producer == DXGI_DEBUG_D3D11)
                            Logger.Log($"D3D11 {Convert(message->Severity)}: {msg} [ {Convert(message->Category)} ]");

                        Free(message);
                    }
                }
            }

            IDXGIInfoQueue.ClearStoredMessages(DXGI_DEBUG_ALL);
        }

        public virtual GraphicsBackend Backend => GraphicsBackend.D3D11;

        public virtual int PlatformScore => 100;

        [SupportedOSPlatform("windows")]
        public virtual IGraphicsDevice CreateGraphicsDevice(bool debug)
        {
            AdapterDesc1 desc;
            IDXGIAdapter.GetDesc1(&desc);
            string name = new(desc.Description);

            Logger.Info("Backend: Using Graphics API: D3D11");
            Logger.Info($"Backend: Using Graphics Device: {name}");
            return new D3D11GraphicsDevice(source, this, debug);
        }

        [SupportedOSPlatform("windows")]
        internal ISwapChain CreateSwapChainForWindow(D3D11GraphicsDevice device, SdlWindow window)
        {
            SwapChainDesc1 desc = new()
            {
                Width = (uint)window.Width,
                Height = (uint)window.Height,
                Format = AutoChooseSwapChainFormat(device.Device, IDXGIOutput),
                BufferCount = 2,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SampleDesc = new(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = (uint)(SwapChainFlag.AllowModeSwitch | SwapChainFlag.AllowTearing)
            };

            SwapChainFullscreenDesc fullscreenDesc = new()
            {
                Windowed = 1,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified,
            };

            nint hwnd = window.GetHWND();
            ComPtr<IDXGISwapChain2> swapChain = default;
            IDXGIFactory.CreateSwapChainForHwnd(device.Device, hwnd, &desc, &fullscreenDesc, IDXGIOutput, ref swapChain);

            return new DXGISwapChain(device, swapChain, (SwapChainFlag)desc.Flags);
        }

        [SupportedOSPlatform("windows")]
        internal ISwapChain CreateSwapChainForWindow(D3D11GraphicsDevice device, Window* window)
        {
            Sdl sdl = Application.sdl;
            SysWMInfo info;
            sdl.GetVersion(&info.Version);
            sdl.GetWindowWMInfo(window, &info);

            int width = 0;
            int height = 0;

            sdl.GetWindowSize(window, &width, &height);

            var Hwnd = info.Info.Win.Hwnd;

            SwapChainDesc1 desc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = AutoChooseSwapChainFormat(device.Device, IDXGIOutput),
                BufferCount = 2,
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SampleDesc = new(1, 0),
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                Flags = (uint)(SwapChainFlag.AllowModeSwitch | SwapChainFlag.AllowTearing)
            };

            SwapChainFullscreenDesc fullscreenDesc = new()
            {
                Windowed = 1,
                RefreshRate = new Rational(0, 1),
                Scaling = ModeScaling.Unspecified,
                ScanlineOrdering = ModeScanlineOrder.Unspecified,
            };

            ComPtr<IDXGISwapChain2> swapChain = default;
            IDXGIFactory.CreateSwapChainForHwnd(device.Device, Hwnd, &desc, &fullscreenDesc, IDXGIOutput, ref swapChain);

            return new DXGISwapChain(device, swapChain, (SwapChainFlag)desc.Flags);
        }

        private ComPtr<IDXGIAdapter4> GetHardwareAdapter(string? name)
        {
            ComPtr<IDXGIAdapter4> selected = null;
            for (uint adapterIndex = 0;
                (ResultCode)IDXGIFactory.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out ComPtr<IDXGIAdapter4> adapter) !=
                ResultCode.DXGI_ERROR_NOT_FOUND;
                adapterIndex++)
            {
                AdapterDesc1 desc;
                adapter.GetDesc1(&desc).ThrowHResult();

                var nameSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(desc.Description);

                // select by adapter name (description)
                if (name != null && nameSpan == name)
                {
                    return adapter;
                }

                if (((AdapterFlag)desc.Flags & AdapterFlag.Software) != AdapterFlag.None)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Release();
                    continue;
                }

                selected = adapter;
            }

            if (selected.Handle == null)
                throw new NotSupportedException();
            return selected;
        }

        private ComPtr<IDXGIOutput6> GetOutput(string? name)
        {
            ComPtr<IDXGIOutput6> selected = null;
            ComPtr<IDXGIOutput6> output = null;

            for (uint outputIndex = 0;
                (ResultCode)IDXGIAdapter.EnumOutputs(outputIndex, ref output) !=
                ResultCode.DXGI_ERROR_NOT_FOUND;
                outputIndex++)

            {
                OutputDesc1 desc;
                output.GetDesc1(&desc).ThrowHResult();

                // select the user chosen display by name.
                var nameSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(desc.DeviceName);
                if (name != null && nameSpan == name)
                {
                    return output;
                }

                // select primary monitor.
                if (desc.DesktopCoordinates.Min == Vector2D<int>.Zero)
                {
                    selected = output;
                }
            }

            return selected;
        }

        private static bool CheckSwapChainFormat(ComPtr<ID3D11Device5> device, Silk.NET.DXGI.Format target)
        {
            FormatSupport formatSupport;
            device.CheckFormatSupport(target, (uint*)&formatSupport).ThrowHResult();
            return formatSupport.HasFlag(FormatSupport.Display | FormatSupport.RenderTarget);
        }

        private static Silk.NET.DXGI.Format ChooseSwapChainFormat(ComPtr<ID3D11Device5> device, Silk.NET.DXGI.Format preferredFormat)
        {
            // Check if the preferred format is supported
            if (CheckSwapChainFormat(device, preferredFormat))
            {
                // Use the preferred format
                return preferredFormat;
            }
            else
            {
                // Fallback to B8G8R8A8_UNorm if the preferred format is not supported
                return Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm;
            }
        }

        private static Silk.NET.DXGI.Format AutoChooseSwapChainFormat(ComPtr<ID3D11Device5> device, ComPtr<IDXGIOutput6> output)
        {
            OutputDesc1 desc;
            output.GetDesc1(&desc).ThrowHResult();

            if (desc.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020)
            {
                return ChooseSwapChainFormat(device, Silk.NET.DXGI.Format.FormatR10G10B10A2Unorm);
            }

            if (desc.ColorSpace == ColorSpaceType.RgbFullG22NoneP709)
            {
                return ChooseSwapChainFormat(device, Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm);
            }

            // If none of the preferred formats is supported, choose a fallback format
            return Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm;
        }
    }
}