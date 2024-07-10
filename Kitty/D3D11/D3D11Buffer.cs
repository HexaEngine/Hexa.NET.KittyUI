﻿namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Direct3D11;
    using ResourceDimension = Graphics.ResourceDimension;

    public unsafe class D3D11Buffer : DeviceChildBase, IBuffer
    {
        internal readonly ID3D11Buffer* buffer;

        internal D3D11Buffer(ID3D11Buffer* buffer, BufferDescription desc)
        {
            this.buffer = buffer;
            nativePointer = new(buffer);
            Description = desc;
            Length = desc.ByteWidth;
        }

        public BufferDescription Description { get; }

        public int Length { get; }

        public ResourceDimension Dimension => ResourceDimension.Buffer;

        protected override void DisposeCore()
        {
            buffer->Release();
        }
    }
}