namespace Hexa.NET.Kitty.Security
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

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

    public unsafe class SecureMemoryLinux
    {
        private static int keyringId = -1;
        private void* _memoryPtr;
        private int _length;
        private int _paddedLength;

        private const int AES_KEY_SIZE = 32; // 256 bits
        private const int AES_NONCE_SIZE = 12; // 96 bits
        private const int AES_TAG_SIZE = 16; // 128 bits

        private const int PROT_NONE = 0x0;
        private const int PROT_READ = 0x1;
        private const int PROT_WRITE = 0x2;
        private const int PROT_READ_WRITE = PROT_READ | PROT_WRITE;

        private const int MAP_PRIVATE = 0x02;
        private const int MAP_ANONYMOUS = 0x20;

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_join_session_keyring(void* name);

        [DllImport("libkeyutils.so")]
        private static extern int add_key(string type, string description, void* payload, uint plen, int ringid);

        [DllImport("libkeyutils.so")]
        private static extern int keyctl(int operation, int key, void* value, IntPtr value_len, int keyring);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int mprotect(void* addr, uint len, int prot);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern void* mmap(void* addr, uint length, int prot, int flags, int fd, int offset);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int munmap(void* addr, uint length);

        static SecureMemoryLinux()
        {
            keyringId = keyctl_join_session_keyring(null);
            if (keyringId < 0)
            {
                throw new InvalidOperationException("Failed to create session keyring.");
            }
        }

        public unsafe void StoreSensitiveData(byte* data, int length)
        {
            _length = length;
            _paddedLength = _length + AES_TAG_SIZE + AES_NONCE_SIZE;

            // Step 1: Allocate memory using mmap
            _memoryPtr = mmap(null, (uint)_paddedLength, PROT_READ_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
            if (_memoryPtr == null)
            {
                throw new InvalidOperationException("Failed to allocate memory.");
            }

            // Step 2: Copy the data into the allocated memory
            Span<byte> memorySpan = new Span<byte>(_memoryPtr, _length);
            new Span<byte>(data, _length).CopyTo(memorySpan);

            // Step 3: Encrypt the memory using AesGcm
            ProtectMemoryContent();

            // Step 4: Protect the memory using mprotect
            ProtectMemory(PROT_NONE);
        }

        public unsafe void AccessSensitiveData(byte* outputBuffer, int length)
        {
            // Step 1: Unprotect the memory using mprotect
            ProtectMemory(PROT_READ_WRITE);

            // Step 2: Decrypt the memory
            UnprotectMemoryContent();

            // Step 3: Copy the data into the output buffer
            Span<byte> outputSpan = new Span<byte>(outputBuffer, length);
            Span<byte> memorySpan = new Span<byte>(_memoryPtr, _length);
            memorySpan.CopyTo(outputSpan);

            // Step 4: Re-encrypt the memory content
            ProtectMemoryContent();

            // Step 5: Protect the memory again
            ProtectMemory(PROT_NONE);
        }

        public void SecureClear()
        {
            // Step 1: Unprotect the memory using mprotect
            ProtectMemory(PROT_READ_WRITE);

            // Step 2: Securely clear the memory
            Span<byte> memorySpan = new Span<byte>(_memoryPtr, _paddedLength);
            memorySpan.Clear();

            // Step 3: Free the memory using munmap
            munmap(_memoryPtr, (uint)_paddedLength);
            _memoryPtr = null;
            _length = 0;
            _paddedLength = 0;
        }

        private void ProtectMemoryContent()
        {
            // Allocate the key and nonce
            Span<byte> key = stackalloc byte[AES_KEY_SIZE];
            Span<byte> nonce = stackalloc byte[AES_NONCE_SIZE];
            Span<byte> tag = stackalloc byte[AES_TAG_SIZE];

            // Retrieve the key from the keyring
            RetrieveKeyFromKeyring(key);

            // Generate a random nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt the memory
            Span<byte> memorySpan = new Span<byte>(_memoryPtr, _length);
            Span<byte> ciphertext = new Span<byte>((byte*)_memoryPtr, _length + AES_TAG_SIZE + AES_NONCE_SIZE);

            // Copy the nonce to the beginning of the ciphertext
            nonce.CopyTo(ciphertext.Slice(0, AES_NONCE_SIZE));

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, memorySpan, ciphertext.Slice(AES_NONCE_SIZE, _length), tag);
                tag.CopyTo(ciphertext.Slice(AES_NONCE_SIZE + _length, AES_TAG_SIZE));
            }
        }

        private void UnprotectMemoryContent()
        {
            // Allocate the key and nonce
            Span<byte> key = stackalloc byte[AES_KEY_SIZE];
            Span<byte> nonce = stackalloc byte[AES_NONCE_SIZE];
            Span<byte> tag = stackalloc byte[AES_TAG_SIZE];

            // Retrieve the key from the keyring
            RetrieveKeyFromKeyring(key);

            // Decrypt the memory
            Span<byte> ciphertext = new Span<byte>(_memoryPtr, _paddedLength);
            Span<byte> memorySpan = new Span<byte>(_memoryPtr, _length);

            // Extract the nonce and tag from the ciphertext
            ciphertext.Slice(0, AES_NONCE_SIZE).CopyTo(nonce);
            ciphertext.Slice(_length + AES_NONCE_SIZE, AES_TAG_SIZE).CopyTo(tag);

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Decrypt(nonce, ciphertext.Slice(AES_NONCE_SIZE, _length), tag, memorySpan);
            }
        }

        private void RetrieveKeyFromKeyring(Span<byte> key)
        {
            if (keyctl(KEYCTL_GET_KEY, keyringId, (void*)key.GetPinnableReference(), (IntPtr)AES_KEY_SIZE, keyringId) < 0)
            {
                throw new InvalidOperationException("Failed to retrieve key from keyring.");
            }
        }

        private void ProtectMemory(int protection)
        {
            if (mprotect(_memoryPtr, (uint)_paddedLength, protection) != 0)
            {
                throw new InvalidOperationException("Failed to protect memory.");
            }
        }
    }
}