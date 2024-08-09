namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using System;
    using System.Numerics;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public unsafe class D3D11GraphicsPipeline : IGraphicsPipeline
    {
        private readonly string dbgName;
        private bool disposedValue;
        protected readonly D3D11GraphicsDevice device;
        protected readonly GraphicsPipelineDesc desc;
        protected ComPtr<ID3D11VertexShader> vs;
        protected ComPtr<ID3D11HullShader> hs;
        protected ComPtr<ID3D11DomainShader> ds;
        protected ComPtr<ID3D11GeometryShader> gs;
        protected ComPtr<ID3D11PixelShader> ps;
        protected ComPtr<ID3D11InputLayout> layout;
        protected ComPtr<ID3D11RasterizerState> rasterizerState;
        protected ComPtr<ID3D11DepthStencilState> depthStencilState;
        protected ComPtr<ID3D11BlendState> blendState;
        protected bool valid;
        protected volatile bool initialized;

        public D3D11GraphicsPipeline(D3D11GraphicsDevice device, GraphicsPipelineDesc desc, string dbgName = "")
        {
            this.device = device;
            this.desc = desc;
            this.dbgName = dbgName;
            Compile();
            PipelineManager.Register(this);
            initialized = true;
        }

        public string DebugName => dbgName;

        public GraphicsPipelineDesc Description => desc;

        public bool IsInitialized => initialized;

        public bool IsValid => valid;

        public GraphicsPipelineState State
        {
            get => desc.State;
            set
            {
            }
        }

        public void Recompile()
        {
            initialized = false;

            if (vs.Handle != null)
            {
                vs.Release();
                vs = null;
            }

            if (hs.Handle != null)
            {
                hs.Release();
                hs = null;
            }

            if (ds.Handle != null)
            {
                ds.Release();
                ds = null;
            }

            if (gs.Handle != null)
            {
                gs.Release();
                gs = null;
            }

            if (ps.Handle != null)
            {
                ps.Release();
                ps = null;
            }

            if (layout.Handle != null)
            {
                layout.Release();
                layout = null;
            }

            Compile(true);
            initialized = true;
        }

        private static bool CanSkipLayout(InputElementDescription[]? inputElements)
        {
            ArgumentNullException.ThrowIfNull(inputElements, nameof(inputElements));

            for (int i = 0; i < inputElements.Length; i++)
            {
                var inputElement = inputElements[i];
                if (inputElement.SemanticName is not "SV_VertexID" and not "SV_InstanceID")
                {
                    return false;
                }
            }

            return true;
        }

        private unsafe void Compile(bool bypassCache = false)
        {
            {
                if (rasterizerState.Handle != null)
                {
                    rasterizerState.Release();
                    rasterizerState = null;
                }

                if (depthStencilState.Handle != null)
                {
                    depthStencilState.Release();
                    depthStencilState = null;
                }

                if (blendState.Handle != null)
                {
                    blendState.Release();
                    blendState = null;
                }

                ID3D11RasterizerState* rs;
                var rsDesc = Helper.Convert(desc.State.Rasterizer);
                device.Device.CreateRasterizerState(&rsDesc, &rs);
                rasterizerState = rs;
                Utils.SetDebugName(rasterizerState, $"{dbgName}.{nameof(rasterizerState)}");

                ID3D11DepthStencilState* ds;
                var dsDesc = Helper.Convert(desc.State.DepthStencil);
                device.Device.CreateDepthStencilState(&dsDesc, &ds);
                depthStencilState = ds;
                Utils.SetDebugName(depthStencilState, $"{dbgName}.{nameof(depthStencilState)}");

                ID3D11BlendState* bs;
                var bsDesc = Helper.Convert(desc.State.Blend);
                device.Device.CreateBlendState(&bsDesc, &bs);
                blendState = bs;
                Utils.SetDebugName(blendState, $"{dbgName}.{nameof(blendState)}");
            }

            if (desc.VertexShader != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFileWithInputSignature(desc.VertexShaderEntrypoint, desc.VertexShaderCode, desc.VertexShader, "vs_5_0", desc.Macros ?? [], &shader, out var elements, out var signature, bypassCache);
                if (shader == null || signature == null || desc.InputElements == null && elements == null)
                {
                    valid = false;
                    return;
                }

                ID3D11VertexShader* vertexShader;
                device.Device.CreateVertexShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &vertexShader);
                vs = vertexShader;
                Utils.SetDebugName(vs, $"{dbgName}.{nameof(vs)}");

                var inputElements = desc.InputElements;
                inputElements ??= elements;

                if (!CanSkipLayout(inputElements))
                {
                    if (inputElements is null)
                    {
                        valid = false;
                        return;
                    }

                    ID3D11InputLayout* il;
                    InputElementDesc* descs = AllocT<InputElementDesc>(inputElements.Length);
                    Helper.Convert(inputElements, descs);
                    device.Device.CreateInputLayout(descs, (uint)inputElements.Length, (void*)signature.BufferPointer, signature.PointerSize, &il);
                    Helper.Free(descs, inputElements.Length);
                    Free(descs);
                    layout = il;

                    Utils.SetDebugName(layout, $"{dbgName}.{nameof(layout)}");
                }
                else
                {
                    layout = default;
                }

                Free(shader);
            }

            if (desc.HullShader != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFile(desc.HullShaderEntrypoint, desc.HullShaderCode, desc.HullShader, "hs_5_0", desc.Macros ?? [], &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11HullShader* hullShader;
                device.Device.CreateHullShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &hullShader);
                hs = hullShader;
                Utils.SetDebugName(hs, $"{dbgName}.{nameof(hs)}");

                Free(shader);
            }

            if (desc.DomainShader != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFile(desc.DomainShaderEntrypoint, desc.DomainShaderCode, desc.DomainShader, "ds_5_0", desc.Macros ?? [], &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11DomainShader* domainShader;
                device.Device.CreateDomainShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &domainShader);
                ds = domainShader;
                Utils.SetDebugName(ds, $"{dbgName}.{nameof(hs)}");

                Free(shader);
            }

            if (desc.GeometryShader != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFile(desc.GeometryShaderEntrypoint, desc.GeometryShaderCode, desc.GeometryShader, "gs_5_0", desc.Macros ?? [], &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11GeometryShader* geometryShader;
                device.Device.CreateGeometryShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &geometryShader);
                gs = geometryShader;
                Utils.SetDebugName(gs, $"{dbgName}.{nameof(gs)}");

                Free(shader);
            }

            if (desc.PixelShader != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFile(desc.PixelShaderEntrypoint, desc.PixelShaderCode, desc.PixelShader, "ps_5_0", desc.Macros ?? [], &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }

                ID3D11PixelShader* pixelShader;
                device.Device.CreatePixelShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &pixelShader);
                ps = pixelShader;
                Utils.SetDebugName(ps, $"{dbgName}.{nameof(ps)}");

                Free(shader);
            }

            valid = true;
        }

        public virtual void BeginDraw(IGraphicsContext context)
        {
            if (context is not D3D11GraphicsContext contextd3d11) return;
            if (!initialized) return;
            if (!valid) return;

            ComPtr<ID3D11DeviceContext3> ctx = contextd3d11.DeviceContext;
            ctx.VSSetShader(vs, (ID3D11ClassInstance**)null, 0);
            ctx.HSSetShader(hs, (ID3D11ClassInstance**)null, 0);
            ctx.DSSetShader(ds, (ID3D11ClassInstance**)null, 0);
            ctx.GSSetShader(gs, (ID3D11ClassInstance**)null, 0);
            ctx.PSSetShader(ps, (ID3D11ClassInstance**)null, 0);

            ctx.RSSetState(rasterizerState);

            var factor = State.BlendFactor;
            float* fac = (float*)&factor;

            ctx.OMSetBlendState(blendState, fac, uint.MaxValue);
            ctx.OMSetDepthStencilState(depthStencilState, desc.State.StencilRef);
            ctx.IASetInputLayout(layout);
            ctx.IASetPrimitiveTopology(Helper.Convert(desc.State.Topology));
        }

        public virtual void SetGraphicsPipeline(ComPtr<ID3D11DeviceContext3> context, Viewport viewport)
        {
            if (!initialized) return;
            if (!valid) return;

            context.VSSetShader(vs, (ID3D11ClassInstance**)null, 0);
            context.HSSetShader(hs, (ID3D11ClassInstance**)null, 0);
            context.DSSetShader(ds, (ID3D11ClassInstance**)null, 0);
            context.GSSetShader(gs, (ID3D11ClassInstance**)null, 0);
            context.PSSetShader(ps, (ID3D11ClassInstance**)null, 0);

            var dViewport = Helper.Convert(viewport);
            context.RSSetViewports(1, &dViewport);
            context.RSSetState(rasterizerState);

            var factor = State.BlendFactor;
            float* fac = (float*)&factor;

            context.OMSetBlendState(blendState, fac, uint.MaxValue);
            context.OMSetDepthStencilState(depthStencilState, desc.State.StencilRef);
            context.IASetInputLayout(layout);
            context.IASetPrimitiveTopology(Helper.Convert(desc.State.Topology));
        }

        public virtual void SetGraphicsPipeline(ComPtr<ID3D11DeviceContext3> context)
        {
            if (!initialized) return;
            if (!valid) return;

            context.VSSetShader(vs, (ID3D11ClassInstance**)null, 0);
            context.HSSetShader(hs, (ID3D11ClassInstance**)null, 0);
            context.DSSetShader(ds, (ID3D11ClassInstance**)null, 0);
            context.GSSetShader(gs, (ID3D11ClassInstance**)null, 0);
            context.PSSetShader(ps, (ID3D11ClassInstance**)null, 0);

            context.RSSetState(rasterizerState);

            var factor = State.BlendFactor;
            float* fac = (float*)&factor;

            context.OMSetBlendState(blendState, fac, uint.MaxValue);
            context.OMSetDepthStencilState(depthStencilState, desc.State.StencilRef);
            context.IASetInputLayout(layout);
            context.IASetPrimitiveTopology(Helper.Convert(desc.State.Topology));
        }

        public static void UnsetGraphicsPipeline(ComPtr<ID3D11DeviceContext3> context)
        {
            context.VSSetShader((ID3D11VertexShader*)null, null, 0);
            context.HSSetShader((ID3D11HullShader*)null, null, 0);
            context.DSSetShader((ID3D11DomainShader*)null, null, 0);
            context.GSSetShader((ID3D11GeometryShader*)null, null, 0);
            context.PSSetShader((ID3D11PixelShader*)null, null, 0);

            context.RSSetState((ID3D11RasterizerState*)null);

            Vector4 factor = default;
            float* fac = (float*)&factor;

            context.OMSetBlendState((ID3D11BlendState*)null, fac, uint.MaxValue);
            context.OMSetDepthStencilState((ID3D11DepthStencilState*)null, 0);
            context.IASetInputLayout((ID3D11InputLayout*)null);
            context.IASetPrimitiveTopology(default);
        }

        public virtual void EndDraw(IGraphicsContext context)
        {
        }

        public void DrawInstanced(IGraphicsContext context, uint vertexCount, uint instanceCount, uint vertexOffset, uint instanceOffset)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context);
            context.DrawInstanced(vertexCount, instanceCount, vertexOffset, instanceOffset);
            EndDraw(context);
        }

        public void DrawIndexedInstanced(IGraphicsContext context, uint indexCount, uint instanceCount, uint indexOffset, int vertexOffset, uint instanceOffset)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context);
            context.DrawIndexedInstanced(indexCount, instanceCount, indexOffset, vertexOffset, instanceOffset);
            EndDraw(context);
        }

        public void DrawInstanced(IGraphicsContext context, IBuffer args, uint stride)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context);
            context.DrawInstancedIndirect(args, stride);
            EndDraw(context);
        }

        public void DrawIndexedInstancedIndirect(IGraphicsContext context, IBuffer args, uint stride)
        {
            if (!initialized) return;
            if (!valid) return;

            BeginDraw(context);
            context.DrawIndexedInstancedIndirect(args, stride);
            EndDraw(context);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                PipelineManager.Unregister(this);

                if (vs.Handle != null)
                {
                    vs.Release();
                    vs = null;
                }

                if (hs.Handle != null)
                {
                    hs.Release();
                    hs = null;
                }

                if (ds.Handle != null)
                {
                    ds.Release();
                    ds = null;
                }

                if (gs.Handle != null)
                {
                    gs.Release();
                    gs = null;
                }

                if (ps.Handle != null)
                {
                    ps.Release();
                    ps = null;
                }

                if (layout.Handle != null)
                {
                    layout.Release();
                    layout = null;
                }

                if (rasterizerState.Handle != null)
                {
                    rasterizerState.Release();
                    rasterizerState = null;
                }

                if (rasterizerState.Handle != null)
                {
                    depthStencilState.Release();
                    depthStencilState = null;
                }

                if (rasterizerState.Handle != null)
                {
                    blendState.Release();
                    blendState = null;
                }

                disposedValue = true;
            }
        }

        ~D3D11GraphicsPipeline()
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
    }
}