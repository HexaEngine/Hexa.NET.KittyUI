﻿namespace Kitty.Graphics
{
    public struct SubresourceData
    {
        public nint DataPointer;
        public int RowPitch;
        public int SlicePitch;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubresourceData"/> struct.
        /// </summary>
        /// <param name="dataPointer">The dataPointer.</param>
        /// <param name="rowPitch">The row pitch.</param>
        /// <param name="slicePitch">The slice pitch.</param>
        public SubresourceData(nint dataPointer, int rowPitch = 0, int slicePitch = 0)
        {
            DataPointer = dataPointer;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubresourceData"/> struct.
        /// </summary>
        /// <param name="dataPointer">The dataPointer.</param>
        /// <param name="rowPitch">The row pitch.</param>
        /// <param name="slicePitch">The slice pitch.</param>
        public unsafe SubresourceData(void* dataPointer, int rowPitch = 0, int slicePitch = 0)
        {
            DataPointer = new nint(dataPointer);
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        public unsafe Span<T> AsSpan<T>(int length) where T : unmanaged
        {
            return new Span<T>(DataPointer.ToPointer(), length);
        }
    }
}