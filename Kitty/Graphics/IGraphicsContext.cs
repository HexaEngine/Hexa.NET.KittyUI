namespace Kitty.Graphics
{
    using Kitty.Mathematics;
    using System.Numerics;

    public interface IGraphicsContext : IDeviceChild
    {
        public IGraphicsDevice Device { get; }

        public void SetGraphicsPipeline(IGraphicsPipeline pipeline, Viewport viewport);

        public void SetGraphicsPipeline(IGraphicsPipeline? pipeline);

        void CopyResource(IResource dst, IResource src);

        unsafe void Write(IBuffer buffer, void* value, int size);

        unsafe void Write(IBuffer buffer, void* value, int size, Map flags);

        unsafe void Write<T>(IBuffer buffer, T* value, int size) where T : unmanaged;

        unsafe void Write<T>(IBuffer buffer, T* value, int size, Map flags) where T : unmanaged;

        public void Write<T>(IBuffer buffer, T value) where T : unmanaged;

        unsafe void Read(IBuffer buffer, void* value, int size);

        unsafe void Read<T>(IBuffer buffer, T* values, uint count) where T : unmanaged;

        public MappedSubresource Map(IResource resource, int subresourceIndex, MapMode mode, MapFlags flags);

        public void Unmap(IResource resource, int subresourceIndex);

        public void SetVertexBuffer(IBuffer? vertexBuffer, uint stride);

        public void SetVertexBuffer(IBuffer? vertexBuffer, uint stride, uint offset);

        public void SetVertexBuffer(uint slot, IBuffer? vertexBuffer, uint stride);

        public void SetVertexBuffer(uint slot, IBuffer? vertexBuffer, uint stride, uint offset);

        public void SetIndexBuffer(IBuffer? indexBuffer, Format format, int offset);

        public void VSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        public void HSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        public void DSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        public void GSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        public void PSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        public void CSSetConstantBuffer(int slot, IBuffer? constantBuffer);

        unsafe void VSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        unsafe void HSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        unsafe void DSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        unsafe void GSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        unsafe void PSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        unsafe void CSSetConstantBuffers(int slot, void** constantBuffers, uint count);

        public void VSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        public void HSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        public void DSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        public void GSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        public void PSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        public void CSSetShaderResource(int slot, IShaderResourceView? shaderResourceView);

        unsafe void VSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        unsafe void HSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        unsafe void DSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        unsafe void GSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        unsafe void PSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        unsafe void CSSetShaderResources(int slot, void** shaderResourceViews, uint count);

        void VSSetSampler(int slot, ISamplerState? sampler);

        void HSSetSampler(int slot, ISamplerState? sampler);

        void DSSetSampler(int slot, ISamplerState? sampler);

        void GSSetSampler(int slot, ISamplerState? sampler);

        void PSSetSampler(int slot, ISamplerState? sampler);

        void CSSetSampler(int slot, ISamplerState? sampler);

        unsafe void VSSetSamplers(int slot, void** samplers, uint count);

        unsafe void HSSetSamplers(int slot, void** samplers, uint count);

        unsafe void DSSetSamplers(int slot, void** samplers, uint count);

        unsafe void GSSetSamplers(int slot, void** samplers, uint count);

        unsafe void PSSetSamplers(int slot, void** samplers, uint count);

        unsafe void CSSetSamplers(int slot, void** samplers, uint count);

        public void ClearRenderTargetView(IRenderTargetView renderTargetView, Vector4 value);

        public void ClearDepthStencilView(IDepthStencilView depthStencilView, DepthStencilClearFlags flags, float depth, byte stencil);

        public void SetRenderTarget(IRenderTargetView? renderTargetView, IDepthStencilView? depthStencilView);

        void SetScissorRect(int x, int y, int z, int w);

        void ClearState();

        void SetViewport(Viewport viewport);

        void SetPrimitiveTopology(PrimitiveTopology topology);

        void DrawInstanced(uint vertexCount, uint instanceCount, uint vertexOffset, uint instanceOffset);

        void DrawIndexedInstanced(uint indexCount, uint instanceCount, uint indexOffset, int vertexOffset, uint instanceOffset);

        void DrawIndexedInstancedIndirect(IBuffer bufferForArgs, uint alignedByteOffsetForArgs);

        unsafe void DrawIndexedInstancedIndirect(void* bufferForArgs, uint alignedByteOffsetForArgs);

        void DrawInstancedIndirect(IBuffer bufferForArgs, uint alignedByteOffsetForArgs);

        unsafe void DrawInstancedIndirect(void* bufferForArgs, uint alignedByteOffsetForArgs);

        void QueryBegin(IQuery query);

        void QueryEnd(IQuery query);

        void QueryGetData(IQuery query);

        void Flush();

        void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ);

        void GenerateMips(IShaderResourceView resourceView);

        void ExecuteCommandList(ICommandList commandList, bool restoreState);

        ICommandList FinishCommandList(bool restoreState);

        void UpdateSubresource(IResource resource, int destSubresource, MappedSubresource subresource);

        unsafe void CSSetUnorderedAccessViews(uint offset, void** views, uint count, int uavInitialCounts = -1);

        unsafe void CSSetUnorderedAccessViews(void** views, uint count, int uavInitialCounts = -1);

        unsafe void SetRenderTargets(void** views, uint count, IDepthStencilView? depthStencilView);

        unsafe void ClearRenderTargetViews(void** rtvs, uint count, Vector4 value);

        void ClearUnorderedAccessViewUint(IUnorderedAccessView uav, uint r, uint g, uint b, uint a);
    }

    public interface IQuery : IDeviceChild
    {
    }

    public interface IPredicate : IDeviceChild
    {
    }

    public interface ICommandList : IDeviceChild
    {
    }
}