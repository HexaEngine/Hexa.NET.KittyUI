namespace Hexa.NET.Kitty.Security
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    public enum ResultCode
    {
        Success = 0,
        FailedToAllocateMemory = 0x1,
        FailedToFreeMemory = 0x2,
        EncryptionFailed = 0x3,
        DecryptionFailed = 0x4,
        KeyRetrieveError = 0x5,
        KeyAddError = 0x6,
        KeyDeleteError = 0x7,
        OutputBufferTooSmall,
        MemoryProtectionError,
        UnknownError,
    }

    /// <summary>
    /// Before you guys ask WHY I'm using unsafe code, it's because using managed memory is way more annoying to deal with you need to pin the memory, and it's not as efficient as using unmanaged memory.
    /// Strings are extremely at risk of being leaked, their immutable nature makes it hard to aquire a pinned pointer, you eventually end up with fixed again. It's just a mess.
    /// </summary>
    public unsafe class SecureMemoryLinux
    {
        private readonly static int keyringId = -1;

        private readonly string _key;
        private int keyId = -1;
        private void* _memoryPtr;
        private int _length;
        private int _paddedLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureMemoryLinux"/> class.
        /// </summary>
        /// <param name="key">The key paramter is used to identify the key in the keyring, not to be confused for the actual encryption key.</param>
        public SecureMemoryLinux(string key)
        {
            _key = key;
        }

        private const int AES_KEY_SIZE = 32; // 256 bits
        private const int AES_NONCE_SIZE = 12; // 96 bits
        private const int AES_TAG_SIZE = 16; // 128 bits

        private const int PROT_NONE = 0x0;
        private const int PROT_READ = 0x1;
        private const int PROT_WRITE = 0x2;
        private const int PROT_READ_WRITE = PROT_READ | PROT_WRITE;

        private const int MAP_PRIVATE = 0x02;
        private const int MAP_LOCKED = 0x20;

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int mprotect(void* addr, uint len, int prot);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern void* mmap(void* addr, uint length, int prot, int flags, int fd, int offset);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int munmap(void* addr, uint length);

        public string Key => _key;

        public int Length => _length;

        public unsafe ResultCode StoreSensitiveData(byte* data, int length)
        {
            _length = length;
            _paddedLength = _length + AES_TAG_SIZE + AES_NONCE_SIZE;

            // Step 1: Allocate memory using mmap
            _memoryPtr = mmap(null, (uint)_paddedLength, PROT_READ_WRITE, MAP_PRIVATE | MAP_LOCKED, -1, 0);
            if (_memoryPtr == null)
            {
                return ResultCode.FailedToAllocateMemory;
            }

            // Step 2: Encrypt the memory using AesGcm (implicit copy)
            var status = ProtectMemoryContent(data, length);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Step 3: Protect the memory using mprotect
            return ProtectMemory(PROT_NONE);
        }

        public unsafe ResultCode AccessSensitiveData(byte* outputBuffer, int length)
        {
            if (length < _length)
            {
                throw new InvalidOperationException("Output buffer is too small.");
            }

            // Step 1: Unprotect the memory using mprotect
            var status = ProtectMemory(PROT_READ_WRITE);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Step 2: Decrypt the memory and rotate the key. (implicit copy)
            status = RetieveDataAndRotate(outputBuffer, length);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Step 3: Protect the memory again
            return ProtectMemory(PROT_NONE);
        }

        public ResultCode SecureClear()
        {
            // Step 1: Destroy the key
            var status = DestroyKey();

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Step 2: Unprotect the memory using mprotect
            status = ProtectMemory(PROT_READ_WRITE);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Step 3: Securely clear the memory, probably not necessary since the memory is encrypted, but it's good practice.
            Span<byte> memorySpan = new(_memoryPtr, _paddedLength);
            memorySpan.Clear();

            // Step 4: Free the memory using munmap
            if (munmap(_memoryPtr, (uint)_paddedLength) != 0)
            {
                return ResultCode.FailedToFreeMemory;
            }

            _memoryPtr = null;
            _length = 0;
            _paddedLength = 0;

            return ResultCode.Success;
        }

        private ResultCode ProtectMemoryContent(byte* inBuffer, int size)
        {
            // Allocate the key and nonce
            byte* key = stackalloc byte[AES_KEY_SIZE];

            // Retrieve the key from the keyring
            var status = RetrieveKeyFromKeyring(key, AES_TAG_SIZE, renew: true);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Encrypt the memory
            Span<byte> plaintext = new(inBuffer, size);
            Span<byte> buffer = new((byte*)_memoryPtr, _length + AES_TAG_SIZE + AES_NONCE_SIZE);

            Span<byte> nonce = buffer[..AES_NONCE_SIZE];
            Span<byte> tag = buffer.Slice(AES_NONCE_SIZE, AES_TAG_SIZE);
            Span<byte> ciphertext = buffer.Slice(AES_NONCE_SIZE + AES_TAG_SIZE, _length);

            // Generate a random nonce
            RandomNumberGenerator.Fill(nonce);

            using var aesGcm = new AesGcm(new Span<byte>(key, AES_KEY_SIZE), AES_TAG_SIZE);

            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            new Span<byte>(key, AES_KEY_SIZE).Clear(); // this is not necessary, since stackalloc get's cleared automatically, but it's good practice.

            return ResultCode.Success;
        }

        private ResultCode RetieveDataAndRotate(byte* outBuffer, int size)
        {
            // Allocate the key and nonce
            byte* key = stackalloc byte[AES_KEY_SIZE];

            // Retrieve the key from the keyring
            var status = RetrieveKeyFromKeyring(key, AES_KEY_SIZE, false);

            if (status != ResultCode.Success)
            {
                return status;
            }

            // Decrypt the memory
            Span<byte> buffer = new(_memoryPtr, _paddedLength);
            Span<byte> plaintext = new(outBuffer, size);

            // Extract the nonce and tag from the ciphertext
            Span<byte> nonce = buffer[..AES_NONCE_SIZE];
            Span<byte> tag = buffer.Slice(AES_NONCE_SIZE, AES_TAG_SIZE);
            Span<byte> ciphertext = buffer.Slice(AES_NONCE_SIZE + AES_TAG_SIZE, _length);

            using var aesGcm = new AesGcm(new Span<byte>(key, AES_KEY_SIZE), AES_TAG_SIZE);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            new Span<byte>(key, AES_KEY_SIZE).Clear(); // this is not necessary, since stackalloc get's cleared automatically, but it's good practice.

            return ProtectMemoryContent(outBuffer, size);
        }

        static SecureMemoryLinux()
        {
            keyringId = keyctl_join_session_keyring(null);
            if (keyringId < 0)
            {
                throw new InvalidOperationException("Failed to create session keyring.");
            }
        }

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_join_session_keyring(void* name);

        [DllImport("libkeyutils.so")]
        private static extern int add_key(string type, string description, void* payload, uint plen, int ringid);

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_revoke(int key);

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_read(int keyId, void* buffer, int size);

        private ResultCode RetrieveKeyFromKeyring(byte* key, int size, bool renew)
        {
            if (keyId == -1 || renew)
            {
                if (keyId != -1)
                {
                    var result = keyctl_revoke(keyId);
                    if (result < 0)
                    {
                        return ResultCode.KeyDeleteError;
                    }
                    keyId = -1;
                }

                RandomNumberGenerator.Fill(new Span<byte>(key, size));
                keyId = add_key("user", _key, key, (uint)size, keyringId);
                if (keyId < 0)
                {
                    return ResultCode.KeyAddError;
                }
                return ResultCode.Success;
            }

            if (keyctl_read(keyId, key, size) < 0)
            {
                return ResultCode.KeyRetrieveError;
            }

            return ResultCode.Success;
        }

        private ResultCode DestroyKey()
        {
            if (keyId != -1)
            {
                var result = keyctl_revoke(keyId);
                if (result < 0)
                {
                    return ResultCode.KeyDeleteError;
                }
                keyId = -1;
            }
            return ResultCode.Success;
        }

        private ResultCode ProtectMemory(int protection)
        {
            if (mprotect(_memoryPtr, (uint)_paddedLength, protection) != 0)
            {
                // this doesn't actually mean that the memory is not encrypted, it just means that the memory is not protected by the OS.
                // This mechanism can be bypassed anyway with root.
                return ResultCode.MemoryProtectionError;
            }
            return ResultCode.Success;
        }
    }
}