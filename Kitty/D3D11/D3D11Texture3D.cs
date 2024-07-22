﻿using Kitty.Graphics;

namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Direct3D11;
    using ResourceDimension = ResourceDimension;

    public unsafe class D3D11Texture3D : DeviceChildBase, ITexture3D
    {
        internal readonly ID3D11Texture3D* texture;

        public D3D11Texture3D(ID3D11Texture3D* texture, Texture3DDescription description)
        {
            this.texture = texture;
            nativePointer = new(texture);
            Description = description;
        }

        public Texture3DDescription Description { get; }

        public ResourceDimension Dimension => ResourceDimension.Texture3D;

        protected override void DisposeCore()
        {
            texture->Release();
        }
    }
}