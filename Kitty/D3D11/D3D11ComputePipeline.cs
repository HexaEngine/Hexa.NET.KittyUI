namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;

    public unsafe class D3D11ComputePipeline : IComputePipeline
    {
        private readonly D3D11GraphicsDevice device;
        private readonly string dbgName;
        private bool valid;
        private bool initialized;
        private ComPtr<ID3D11ComputeShader> cs;
        private ComputePipelineDesc desc;
        private bool disposedValue;

        public D3D11ComputePipeline(D3D11GraphicsDevice device, ComputePipelineDesc desc, string dbgName)
        {
            PipelineManager.Register(this);
            this.dbgName = dbgName;
            this.device = device;
            this.desc = desc;
            Compile();
            initialized = true;
        }

        public string DebugName => dbgName;

        public ComputePipelineDesc Desc => desc;

        public bool IsInitialized => initialized;

        public bool IsValid => valid;

        public ShaderMacro[]? Macros { get => desc.Macros; set => desc.Macros = value; }

        public virtual void BeginDispatch(IGraphicsContext context)
        {
            if (!valid) return;
            if (!initialized) return;
            ((D3D11GraphicsContext)context).DeviceContext.CSSetShader(cs, null, 0);
        }

        public virtual void BeginDispatch(ID3D11DeviceContext1* context)
        {
            if (!valid) return;
            if (!initialized) return;
            context->CSSetShader(cs, null, 0);
        }

        public void Dispatch(IGraphicsContext context, int x, int y, int z)
        {
            if (!valid) return;
            if (!initialized) return;
            BeginDispatch(context);
            context.Dispatch(x, y, z);
            EndDispatch(context);
        }

        public void Dispatch(ID3D11DeviceContext1* context, uint x, uint y, uint z)
        {
            if (!valid) return;
            if (!initialized) return;
            BeginDispatch(context);
            context->Dispatch(x, y, z);
            EndDispatch(context);
        }

        public virtual void EndDispatch(IGraphicsContext context)
        {
            if (!valid) return;
            if (!initialized) return;
            context.ClearState();
        }

        public virtual void EndDispatch(ID3D11DeviceContext1* context)
        {
            if (!valid) return;
            if (!initialized) return;
            context->ClearState();
        }

        public void Recompile()
        {
            initialized = false;

            if (cs.Handle != null)
            {
                cs.Release();
                cs = null;
            }

            Compile(true);

            initialized = true;
        }

        private unsafe void Compile(bool bypassCache = false)
        {
            ShaderMacro[] macros = GetShaderMacros();

            if (desc.Path != null)
            {
                Shader* shader;
                D3D11GraphicsDevice.Compiler.GetShaderOrCompileFile(desc.Entry, desc.Path, "cs_5_0", macros, &shader, bypassCache);
                if (shader == null)
                {
                    valid = false;
                    return;
                }
                ID3D11ComputeShader* computeShader;
                device.Device.CreateComputeShader(shader->Bytecode, shader->Length, (ID3D11ClassLinkage*)null, &computeShader);
                cs = computeShader;
                Utils.SetDebugName(cs, dbgName);
                Free(shader);
                valid = true;
            }
        }

        protected virtual ShaderMacro[] GetShaderMacros()
        {
            return desc.Macros ?? [];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                PipelineManager.Unregister(this);

                if (cs.Handle != null)
                {
                    cs.Release();
                    cs = null;
                }

                disposedValue = true;
            }
        }

        ~D3D11ComputePipeline()
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