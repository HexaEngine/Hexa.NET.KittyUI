namespace Hexa.NET.KittyUI.Security
{
    using System;
    using System.Runtime.InteropServices;

    public unsafe class SecureMemoryWin
    {
        private void* _memoryPtr;
        private int _length;
        private int _paddedLength;

        private const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
        private const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern void* VirtualAlloc(void* lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool VirtualFree(void* lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("crypt32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool CryptProtectMemory(void* pDataIn, uint cbDataIn, uint dwFlags);

        [DllImport("crypt32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool CryptUnprotectMemory(void* pDataIn, uint cbDataIn, uint dwFlags);

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;

        public unsafe void StoreSensitiveData(byte* data, int length)
        {
            _length = length;
            _paddedLength = (_length + (int)CRYPTPROTECTMEMORY_BLOCK_SIZE - 1) & ~((int)CRYPTPROTECTMEMORY_BLOCK_SIZE - 1);

            // Step 1: Allocate unmanaged memory
            _memoryPtr = VirtualAlloc(null, (uint)_paddedLength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (_memoryPtr == null)
            {
                throw new InvalidOperationException("Failed to allocate memory.");
            }

            // Step 2: Copy the data into the allocated memory
            Memcpy(data, _memoryPtr, _length, _length);

            // Zero out any padded bytes
            if (_paddedLength > _length)
            {
                ZeroMemory((byte*)_memoryPtr + _length, _paddedLength - _length);
            }

            // Step 3: Encrypt the memory using CryptProtectMemory
            ProtectMemoryContent();

            for (int i = 0; i < _length; i++)
            {
                Console.WriteLine(((byte*)_memoryPtr)[i]);
            }
        }

        public unsafe void AccessSensitiveData(byte* outputBuffer, int length)
        {
            // Step 1: Decrypt the memory using CryptUnprotectMemory
            UnprotectMemoryContent();

            // Step 2: Copy the data into the output buffer
            Memcpy(_memoryPtr, outputBuffer, length, _length);

            // Step 3: Re-encrypt the memory content
            ProtectMemoryContent();
        }

        public void SecureClear()
        {
            // Step 1: Decrypt the memory using CryptUnprotectMemory
            UnprotectMemoryContent();

            // Step 2: Securely clear the memory
            ZeroMemory(_memoryPtr, _paddedLength);
            VirtualFree(_memoryPtr, 0, MEM_RELEASE);
            _memoryPtr = null;
            _length = 0;
            _paddedLength = 0;
        }

        private void ProtectMemoryContent()
        {
            if (!CryptProtectMemory(_memoryPtr, (uint)_paddedLength, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new InvalidOperationException("Failed to protect memory content.");
            }
        }

        private void UnprotectMemoryContent()
        {
            if (!CryptUnprotectMemory(_memoryPtr, (uint)_paddedLength, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                throw new InvalidOperationException("Failed to unprotect memory content.");
            }
        }
    }
}