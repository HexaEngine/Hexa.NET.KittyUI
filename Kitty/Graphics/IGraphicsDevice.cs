namespace Kitty.Graphics
{
    using Kitty.Mathematics;
    using Kitty.Windows;
    using Silk.NET.SDL;
    using System;
    using System.Runtime.CompilerServices;
    using Window = Silk.NET.SDL.Window;

    public interface IGraphicsDevice : IDeviceChild
    {
        /// <summary>
        /// The immediate context of this device
        /// </summary>
        public IGraphicsContext Context { get; }

        ITextureLoader TextureLoader { get; }

        public ISwapChain CreateSwapChain(SdlWindow window);

        unsafe ISwapChain CreateSwapChain(Window* window);

        /// <summary>
        /// Creates a <see cref="IBuffer"/> with the given <see cref="BufferDescription"/>
        /// </summary>
        /// <param name="description">The <see cref="BufferDescription"/> that describes the <see cref="IBuffer"/></param>
        /// <returns>The <see cref="IBuffer"/></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public IBuffer CreateBuffer(BufferDescription description);

        /// <summary>
        /// Creates a <see cref="IBuffer"/> with the given <see cref="BufferDescription"/>
        /// </summary>
        /// <param name="description">The <see cref="BufferDescription"/> that describes the <see cref="IBuffer"/></param>
        /// <returns>The <see cref="IBuffer"/></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        unsafe IBuffer CreateBuffer(void* src, uint length, BufferDescription description);

        unsafe IBuffer CreateBuffer<T>(T* values, uint count, BufferDescription description) where T : unmanaged;

        unsafe IBuffer CreateBuffer<T>(T* values, uint count, BindFlags bindFlags, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = ResourceMiscFlag.None) where T : unmanaged;

        public IDepthStencilView CreateDepthStencilView(IResource resource, DepthStencilViewDescription description);

        public IDepthStencilView CreateDepthStencilView(IResource resource);

        public IRenderTargetView CreateRenderTargetView(IResource resource, Viewport viewport);

        public IRenderTargetView CreateRenderTargetView(IResource resource, RenderTargetViewDescription description, Viewport viewport);

        public IShaderResourceView CreateShaderResourceView(IResource resource);

        public IShaderResourceView CreateShaderResourceView(IResource texture, ShaderResourceViewDescription description);

        public IShaderResourceView CreateShaderResourceView(IBuffer buffer);

        public IShaderResourceView CreateShaderResourceView(IBuffer buffer, ShaderResourceViewDescription description);

        public ISamplerState CreateSamplerState(SamplerDescription sampler);

        public ITexture1D CreateTexture1D(Texture1DDescription description);

        public ITexture2D CreateTexture2D(Texture2DDescription description);

        public ITexture3D CreateTexture3D(Texture3DDescription description);

        public ITexture1D CreateTexture1D(Texture1DDescription description, SubresourceData[]? subresources);

        public ITexture2D CreateTexture2D(Texture2DDescription description, SubresourceData[]? subresources);

        public ITexture3D CreateTexture3D(Texture3DDescription description, SubresourceData[]? subresources);

        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None);

        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, int sampleCount = 1, int sampleQuality = 0, ResourceMiscFlag misc = ResourceMiscFlag.None);

        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags = BindFlags.ShaderResource, Usage usage = Usage.Default, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag misc = ResourceMiscFlag.None);

        public ITexture1D CreateTexture1D(Format format, int width, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag none);

        public ITexture2D CreateTexture2D(Format format, int width, int height, int arraySize, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag none);

        public ITexture3D CreateTexture3D(Format format, int width, int height, int depth, int mipLevels, SubresourceData[]? subresources, BindFlags bindFlags, ResourceMiscFlag none);

        IQuery CreateQuery();

        IQuery CreateQuery(Query type);

        IGraphicsContext CreateDeferredContext();

        IUnorderedAccessView CreateUnorderedAccessView(IResource resource, UnorderedAccessViewDescription description);

        IGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0);

        IComputePipeline CreateComputePipeline(ComputePipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0);

        Task<IGraphicsPipeline> CreateGraphicsPipelineAsync(GraphicsPipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return Task.Factory.StartNew(() => CreateGraphicsPipeline(desc, filename, line));
        }

        Task<IComputePipeline> CreateComputePipelineAsync(ComputePipelineDesc desc, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0)
        {
            return Task.Factory.StartNew(() => CreateComputePipeline(desc, filename, line));
        }
    }
}