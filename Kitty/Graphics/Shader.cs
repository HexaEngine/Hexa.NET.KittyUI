namespace Kitty.Graphics
{
    using Hexa.NET.Utilities;
    using System.Runtime.InteropServices;

    public unsafe struct Shader : IFreeable
    {
        public byte* Bytecode;
        public nuint Length;

        public void CopyTo(Span<byte> span)
        {
            fixed (byte* ptr = span)
            {
                Buffer.MemoryCopy(Bytecode, ptr, Length, Length);
            }
        }

        public Shader* Clone()
        {
            Shader* result = AllocT<Shader>();
            result->Bytecode = AllocCopyT(Bytecode, (int)Length);
            result->Length = Length;
            return result;
        }

        public static Shader* CreateFrom(byte[] bytes)
        {
            Shader* result = AllocT<Shader>();
            fixed (byte* ptr = bytes)
                result->Bytecode = AllocCopyT(ptr, bytes.Length);
            result->Length = (nuint)bytes.Length;
            return result;
        }

        public void Release()
        {
            Marshal.FreeHGlobal((nint)Bytecode);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[Length];
            fixed (byte* ptr = bytes)
            {
                MemcpyT(Bytecode, ptr, (uint)Length, (uint)Length);
            }
            return bytes;
        }

        public readonly Span<byte> AsSpan()
        {
            return new(Bytecode, (int)Length);
        }
    }
}