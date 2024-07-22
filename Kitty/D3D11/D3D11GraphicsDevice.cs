namespace Kitty.D3D11
{
    using Kitty.Debugging;
    using Kitty.Graphics;
    using Kitty.Windows;
    using Silk.NET.Core.Contexts;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using D3D11SubresourceData = Silk.NET.Direct3D11.SubresourceData;
    using Format = Graphics.Format;
    using Query = Graphics.Query;
    using ResourceMiscFlag = Graphics.ResourceMiscFlag;
    using SubresourceData = Graphics.SubresourceData;
    using Usage = Graphics.Usage;
    using Viewport = Mathematics.Viewport;
    using Window = Silk.NET.SDL.Window;

    public unsafe partial class D3D11GraphicsDevice : IGraphicsDevice
    {
        internal readonly D3D11 D3D11;

        protected readonly DXGIAdapterD3D11 adapter;
        protected bool disposedValue;

        public static readonly ShaderCompiler Compiler;

        public ComPtr<ID3D11Device5> Device;
        public ComPtr<ID3D11DeviceContext3> DeviceContext;

        internal ComPtr<ID3D11Debug> DebugDevice;

        static D3D11GraphicsDevice()
        {
            Compiler = new();
        }

#nullable disable

        protected D3D11GraphicsDevice(INativeWindowSource window, DXGIAdapterD3D11 adapter)
        {
            this.adapter = adapter;
            D3D11 = D3D11.GetApi(window);
            TextureLoader = new D3D11TextureLoader(this);
        }

#nullable restore

        [SupportedOSPlatform("windows")]
        public D3D11GraphicsDevice(INativeWindowSource window, DXGIAdapterD3D11 adapter, bool debug)
        {
            this.adapter = adapter;
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

            D3D11.CreateDevice((IDXGIAdapter*)adapter.IDXGIAdapter.Handle, D3DDriverType.Unknown, nint.Zero, (uint)flags, levels, (uint)levelsArr.Length, D3D11.SdkVersion, &tempDevice, &level, &tempContext).ThrowHResult();
            Level = level;
            tempDevice->QueryInterface(out Device);
            tempContext->QueryInterface(out DeviceContext);

            tempDevice->Release();
            tempContext->Release();

            NativePointer = new(Device.Handle);

#if DEBUG
            if (debug)
            {
                Device.QueryInterface(out DebugDevice);
            }
#endif

            Context = new D3D11GraphicsContext(this);
            TextureLoader = new D3D11TextureLoader(this);
        }

        public IGraphicsContext Context { get; protected set; }

        public nint NativePointer { get; protected set; }

        public string? DebugName { get; set; } = string.Empty;

        public D3DFeatureLevel Level { get; protected set; }

        public bool IsDisposed => disposedValue;

        public event EventHandler? OnDisposed;

        public ISwapChain? SwapChain { get; protected set; }

        [SupportedOSPlatform("windows")]
        public ISwapChain CreateSwapChain(SdlWindow window)
        {
            return adapter.CreateSwapChainForWindow(this, window);
        }

        [SupportedOSPlatform("windows")]
        public ISwapChain CreateSwapChain(Window* window)
        {
            return adapter.CreateSwapChainForWindow(this, window);
        }

        public ITextureLoader TextureLoader { get; }

        public IComputePipeline CreateComputePipeline(ComputePipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return new D3D11ComputePipeline(this, desc, $"({nameof(D3D11ComputePipeline)}: {filename}, Line:{line})");
        }

        public IGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return new D3D11GraphicsPipeline(this, desc, $"({nameof(D3D11GraphicsPipeline)}: {filename}, Line:{line})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer(BufferDescription description)
        {
            ID3D11Buffer* buffer;
            BufferDesc desc = Helper.Convert(description);
            Device.CreateBuffer(&desc, (D3D11SubresourceData*)null, &buffer).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        public IBuffer CreateBuffer(void* src, uint length, BufferDescription description)
        {
            ID3D11Buffer* buffer;
            description.ByteWidth = (int)length;
            BufferDesc desc = Helper.Convert(description);
            var data = Helper.Convert(new SubresourceData(src, description.ByteWidth));
            Device.CreateBuffer(&desc, &data, &buffer).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T* values, uint count, BufferDescription description) where T : unmanaged
        {
            uint size = (uint)(sizeof(T) * count);
            ID3D11Buffer* buffer;
            description.ByteWidth = (int)size;
            BufferDesc desc = Helper.Convert(description);
            var data = Helper.Convert(new SubresourceData(values, description.ByteWidth));
            Device.CreateBuffer(&desc, &data, &buffer).ThrowHResult();
            return new D3D11Buffer(buffer, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBuffer CreateBuffer<T>(T* values, uint count, BindFlags bindFlags, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = ResourceMiscFlag.None) where T : unmanaged
        {
            BufferDescription description = new(0, bindFlags, usage, cpuAccessFlags, miscFlags);
            return CreateBuffer(values, count, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDepthStencilView CreateDepthStencilView(IResource resource)
        {
            DepthStencilViewDescription description;
            if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                DepthStencilViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                        dimension = DepthStencilViewDimension.Texture2DMultisampledArray;
                    else
                        dimension = DepthStencilViewDimension.Texture2DArray;
                }
                else
                    dimension = DepthStencilViewDimension.Texture2D;
                description = new(texture2d, dimension);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateDepthStencilView(resource, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDepthStencilView CreateDepthStencilView(IResource resource, DepthStencilViewDescription description)
        {
            ID3D11DepthStencilView* view;
            var desc = Helper.Convert(description);
            Device.CreateDepthStencilView((ID3D11Resource*)resource.NativePointer, &desc, &view).ThrowHResult();
            return new D3D11DepthStencilView(view, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IRenderTargetView CreateRenderTargetView(IResource resource, Viewport viewport)
        {
            RenderTargetViewDescription description;
            if (resource is IBuffer)
            {
                throw new NotImplementedException();
            }
            else if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                RenderTargetViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                        dimension = RenderTargetViewDimension.Texture2DMultisampledArray;
                    else
                        dimension = RenderTargetViewDimension.Texture2DArray;
                }
                else
                    dimension = RenderTargetViewDimension.Texture2D;
                description = new(texture2d, dimension);
            }
            else if (resource is ITexture3D texture3d)
            {
                description = new(texture3d);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateRenderTargetView(resource, description, viewport);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IRenderTargetView CreateRenderTargetView(IResource resource, RenderTargetViewDescription description, Viewport viewport)
        {
            ID3D11RenderTargetView* rtv;
            var desc = Helper.Convert(description);
            Device.CreateRenderTargetView((ID3D11Resource*)resource.NativePointer, &desc, &rtv).ThrowHResult();
            return new D3D11RenderTargetView(rtv, description, viewport);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISamplerState CreateSamplerState(SamplerDescription description)
        {
            ID3D11SamplerState* sampler;
            var desc = Helper.Convert(description);
            Device.CreateSamplerState(&desc, &sampler).ThrowHResult();
            return new D3D11SamplerState(sampler, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IResource resource)
        {
            ShaderResourceViewDescription description;
            if (resource is IBuffer)
            {
                throw new NotImplementedException();
            }
            else if (resource is ITexture1D texture1d)
            {
                description = new(texture1d, texture1d.Description.ArraySize > 1);
            }
            else if (resource is ITexture2D texture2d)
            {
                ShaderResourceViewDimension dimension;
                if (texture2d.Description.ArraySize > 1)
                {
                    if (texture2d.Description.SampleDescription.Count > 1)
                        dimension = ShaderResourceViewDimension.Texture2DMultisampledArray;
                    else
                        dimension = ShaderResourceViewDimension.Texture2DArray;
                }
                else
                    dimension = ShaderResourceViewDimension.Texture2D;
                if (texture2d.Description.MiscFlags.HasFlag(ResourceMiscFlag.TextureCube))
                {
                    dimension = texture2d.Description.ArraySize / 6 > 1 ? ShaderResourceViewDimension.TextureCubeArray : ShaderResourceViewDimension.TextureCube;
                }
                description = new(texture2d, dimension);
            }
            else if (resource is ITexture3D texture3d)
            {
                description = new(texture3d);
            }
            else
            {
                throw new NotSupportedException();
            }

            return CreateShaderResourceView(resource, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IResource resource, ShaderResourceViewDescription description)
        {
            ID3D11ShaderResourceView* srv;
            var desc = Helper.Convert(description);
            Device.CreateShaderResourceView((ID3D11Resource*)resource.NativePointer, &desc, &srv).ThrowHResult();
            return new D3D11ShaderResourceView(srv, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IBuffer buffer)
        {
            ID3D11ShaderResourceView* srv;
            Device.CreateShaderResourceView((ID3D11Resource*)buffer.NativePointer, (ShaderResourceViewDesc*)null, &srv).ThrowHResult();
            return new D3D11ShaderResourceView(srv, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IShaderResourceView CreateShaderResourceView(IBuffer buffer, ShaderResourceViewDescription description)
        {
            ID3D11ShaderResourceView* srv;
            var desc = Helper.Convert(description);
            Device.CreateShaderResourceView((ID3D11Resource*)buffer.NativePointer, &desc, &srv).ThrowHResult();
            return new D3D11ShaderResourceView(srv, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Texture1DDescription description)
        {
            ID3D11Texture1D* texture;
            Texture1DDesc desc = Helper.Convert(description);
            Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Texture1DDescription description, SubresourceData[]? subresources)
        {
            ID3D11Texture1D* texture;
            Texture1DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture1D(&desc, data, &texture).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }
            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture1D(format, width, arraySize, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture1DDescription description = new(format, width, arraySize, mipLevels, bindFlags, usage, cpuAccessFlags, misc);
            ID3D11Texture1D* texture;
            Texture1DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture1D(&desc, data, &texture).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture1D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }

            return new D3D11Texture1D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Texture2DDescription description)
        {
            ID3D11Texture2D* texture;
            Texture2DDesc desc = Helper.Convert(description);
            Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Texture2DDescription description, SubresourceData[]? subresources)
        {
            ID3D11Texture2D* texture;
            Texture2DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture2D(&desc, data, &texture).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }
            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture2D(format, width, height, arraySize, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, 1, 0, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, int sampleCount = 1, int sampleQuality = 0, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture2DDescription description = new(format, width, height, arraySize, mipLevels, bindFlags, usage, cpuAccessFlags, sampleCount, sampleQuality, misc);
            ID3D11Texture2D* texture;
            Texture2DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture2D(&desc, data, &texture).ThrowHResult();
                Free(data);
            }
            else
            {
                Device.CreateTexture2D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }

            return new D3D11Texture2D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Texture3DDescription description)
        {
            ID3D11Texture3D* texture;
            Texture3DDesc desc = Helper.Convert(description);
            Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            return new D3D11Texture3D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Texture3DDescription description, SubresourceData[]? subresources)
        {
            ID3D11Texture3D* texture;
            Texture3DDesc desc = Helper.Convert(description);
            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture3D(&desc, data, &texture).ThrowHResult();
            }
            else
            {
                Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }
            return new D3D11Texture3D(texture, description);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag misc)
        {
            return CreateTexture3D(format, width, height, depth, mipLevels, subresources, bindFlags, Usage.Default, CpuAccessFlags.None, misc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None)
        {
            Texture3DDescription description = new(format, width, height, depth, mipLevels, bindFlags, usage, cpuAccessFlags, misc);
            ID3D11Texture3D* texture;
            Texture3DDesc desc = Helper.Convert(description);

            if (subresources != null)
            {
                D3D11SubresourceData* data = AllocT<D3D11SubresourceData>(subresources.Length);
                Helper.Convert(subresources, data);
                Device.CreateTexture3D(&desc, data, &texture).ThrowHResult();
            }
            else
            {
                Device.CreateTexture3D(&desc, (D3D11SubresourceData*)null, &texture).ThrowHResult();
            }

            return new D3D11Texture3D(texture, description);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OnDisposed?.Invoke(this, EventArgs.Empty);
                SwapChain?.Dispose();
                Context.Dispose();
                Device.Release();

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

                if (DebugDevice.Handle != null)
                {
                    DebugDevice.ReportLiveDeviceObjects(RldoFlags.Detail | RldoFlags.IgnoreInternal);
                    DebugDevice.Release();
                }

                LeakTracer.ReportLiveInstances();

                D3D11.Dispose();

                disposedValue = true;
            }
        }

        ~D3D11GraphicsDevice()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQuery CreateQuery()
        {
            return CreateQuery(Query.Event);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQuery CreateQuery(Query type)
        {
            ID3D11Query* query;
            QueryDesc desc = new(Helper.Convert(type), 0);
            Device.CreateQuery(&desc, &query);
            return new D3D11Query(query);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IGraphicsContext CreateDeferredContext()
        {
            ID3D11DeviceContext3* context;
            Device.CreateDeferredContext3(0, &context);
            return new D3D11GraphicsContext(this, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IUnorderedAccessView CreateUnorderedAccessView(IResource resource, UnorderedAccessViewDescription description)
        {
            ID3D11UnorderedAccessView* view;
            var desc = Helper.Convert(description);
            Device.CreateUnorderedAccessView((ID3D11Resource*)resource.NativePointer, &desc, &view);
            return new D3D11UnorderedAccessView(view, description);
        }
    }
}