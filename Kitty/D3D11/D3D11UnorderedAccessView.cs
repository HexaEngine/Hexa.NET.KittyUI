﻿namespace Kitty.D3D11
{
    using Kitty.Graphics;
    using Silk.NET.Direct3D11;
    using System;

    public unsafe class D3D11UnorderedAccessView : DeviceChildBase, IUnorderedAccessView
    {
        private readonly ID3D11UnorderedAccessView* uva;

        public D3D11UnorderedAccessView(ID3D11UnorderedAccessView* uva, UnorderedAccessViewDescription description)
        {
            this.uva = uva;
            nativePointer = (nint)uva;
            Description = description;
        }

        public UnorderedAccessViewDescription Description { get; }

        protected override void DisposeCore()
        {
            uva->Release();
        }
    }
}