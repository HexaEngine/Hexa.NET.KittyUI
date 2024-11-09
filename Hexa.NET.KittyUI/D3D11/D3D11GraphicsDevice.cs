namespace Hexa.NET.KittyUI.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using Hexa.NET.KittyUI.Debugging;
    using Hexa.NET.KittyUI.Windows;
    using HexaGen.Runtime.COM;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    public static unsafe partial class D3D11GraphicsDevice
    {
        public static ComPtr<ID3D11Device5> Device;
        public static ComPtr<ID3D11DeviceContext3> DeviceContext;

        internal static ComPtr<ID3D11Debug> DebugDevice;

        [SupportedOSPlatform("windows")]
        public static void Init(IWindow window, bool debug)
        {
            FeatureLevel[] levelsArr =
           [
                FeatureLevel.Level111,
               FeatureLevel.Level110
           ];

            CreateDeviceFlag flags = CreateDeviceFlag.BgraSupport;

            if (debug)
                flags |= CreateDeviceFlag.Debug;

            ID3D11Device* tempDevice;
            ID3D11DeviceContext* tempContext;

            FeatureLevel level = 0;
            FeatureLevel* levels = (FeatureLevel*)Unsafe.AsPointer(ref levelsArr[0]);

            D3D11.CreateDevice((IDXGIAdapter*)D3D11Adapter.IDXGIAdapter.Handle, DriverType.Unknown, nint.Zero, (uint)flags, levels, (uint)levelsArr.Length, D3D11.D3D11_SDK_VERSION, &tempDevice, &level, &tempContext).ThrowIf();
            Level = level;
            tempDevice->QueryInterface(out Device);
            tempContext->QueryInterface(out DeviceContext);

            tempDevice->Release();
            tempContext->Release();

#if DEBUG
            if (debug)
            {
                Device.QueryInterface(out DebugDevice);
            }
#endif
        }

        public static FeatureLevel Level { get; set; }

        public static void Shutdown()
        {
            DeviceContext.Release();
            Device.Release();

            DeviceContext = default;
            Device = default;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

            if (DebugDevice.Handle != null)
            {
                DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail | RldoFlags.IgnoreInternal);
                DebugDevice.Release();
                DebugDevice = default;
            }

            LeakTracer.ReportLiveInstances();
        }
    }
}