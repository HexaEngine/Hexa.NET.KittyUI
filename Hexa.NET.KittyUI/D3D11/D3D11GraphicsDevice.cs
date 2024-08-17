namespace Hexa.NET.KittyUI.D3D11
{
    using Hexa.NET.KittyUI.Debugging;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    public static unsafe partial class D3D11GraphicsDevice
    {
        internal static D3D11 D3D11;

        public static ComPtr<ID3D11Device5> Device;
        public static ComPtr<ID3D11DeviceContext3> DeviceContext;

        internal static ComPtr<ID3D11Debug> DebugDevice;

        [SupportedOSPlatform("windows")]
        public static void Init(INativeWindowSource window, bool debug)
        {
            D3D11 = D3D11.GetApi(window);
            D3DFeatureLevel[] levelsArr =
           [
                D3DFeatureLevel.Level111,
               D3DFeatureLevel.Level110
           ];

            CreateDeviceFlag flags = CreateDeviceFlag.BgraSupport;

            if (debug)
                flags |= CreateDeviceFlag.Debug;

            ID3D11Device* tempDevice;
            ID3D11DeviceContext* tempContext;

            D3DFeatureLevel level = 0;
            D3DFeatureLevel* levels = (D3DFeatureLevel*)Unsafe.AsPointer(ref levelsArr[0]);

            D3D11.CreateDevice((IDXGIAdapter*)D3D11Adapter.IDXGIAdapter.Handle, D3DDriverType.Unknown, nint.Zero, (uint)flags, levels, (uint)levelsArr.Length, D3D11.SdkVersion, &tempDevice, &level, &tempContext).ThrowHResult();
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

        public static D3DFeatureLevel Level { get; set; }

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

            D3D11.Dispose();
        }
    }
}