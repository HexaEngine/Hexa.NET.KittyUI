namespace Hexa.NET.Kitty.Security
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

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
        private const int MAP_LOCKED = 0x20;

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int mprotect(void* addr, uint len, int prot);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern void* mmap(void* addr, uint length, int prot, int flags, int fd, int offset);

        [DllImport("libc.so.6", SetLastError = true, ExactSpelling = true)]
        private static extern int munmap(void* addr, uint length);

        public unsafe void StoreSensitiveData(byte* data, int length)
        {
            _length = length;
            _paddedLength = _length + AES_TAG_SIZE + AES_NONCE_SIZE;

            // Step 1: Allocate memory using mmap
            _memoryPtr = mmap(null, (uint)_paddedLength, PROT_READ_WRITE, MAP_PRIVATE | MAP_LOCKED, -1, 0);
            if (_memoryPtr == null)
            {
                throw new InvalidOperationException("Failed to allocate memory.");
            }

            // Step 3: Encrypt the memory using AesGcm (implicit copy)
            ProtectMemoryContent(data, length);

            // Step 4: Protect the memory using mprotect
            ProtectMemory(PROT_NONE);
        }

        public unsafe void AccessSensitiveData(byte* outputBuffer, int length)
        {
            // Step 1: Unprotect the memory using mprotect
            ProtectMemory(PROT_READ_WRITE);

            // Step 2: Decrypt the memory and rotate the key. (implicit copy)
            RetieveDataAndRotate(outputBuffer, length);

            // Step 5: Protect the memory again
            ProtectMemory(PROT_NONE);
        }

        public void SecureClear()
        {
            // Step 1: Unprotect the memory using mprotect
            ProtectMemory(PROT_READ_WRITE);

            // Step 2: Securely clear the memory
            Span<byte> memorySpan = new(_memoryPtr, _paddedLength);
            memorySpan.Clear();

            // Step 3: Free the memory using munmap
            munmap(_memoryPtr, (uint)_paddedLength);
            _memoryPtr = null;
            _length = 0;
            _paddedLength = 0;
        }

        private void ProtectMemoryContent(byte* inBuffer, int size)
        {
            // Allocate the key and nonce
            byte* key = stackalloc byte[AES_KEY_SIZE];

            // Retrieve the key from the keyring
            RetrieveKeyFromKeyring(key, AES_TAG_SIZE, renew: true);

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
        }

        private void RetieveDataAndRotate(byte* outBuffer, int size)
        {
            // Allocate the key and nonce
            byte* key = stackalloc byte[AES_KEY_SIZE];

            // Retrieve the key from the keyring
            RetrieveKeyFromKeyring(key, AES_KEY_SIZE, false);

            // Decrypt the memory
            Span<byte> buffer = new Span<byte>(_memoryPtr, _paddedLength);
            Span<byte> plaintext = new Span<byte>(outBuffer, size);

            // Extract the nonce and tag from the ciphertext
            Span<byte> nonce = buffer[..AES_NONCE_SIZE];
            Span<byte> tag = buffer.Slice(AES_NONCE_SIZE, AES_TAG_SIZE);
            Span<byte> ciphertext = buffer.Slice(AES_NONCE_SIZE + AES_TAG_SIZE, _length);

            using var aesGcm = new AesGcm(new Span<byte>(key, AES_KEY_SIZE), AES_TAG_SIZE);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            new Span<byte>(key, AES_KEY_SIZE).Clear(); // this is not necessary, since stackalloc get's cleared automatically, but it's good practice.

            ProtectMemoryContent(outBuffer, size);
        }

        static SecureMemoryLinux()
        {
            keyringId = keyctl_join_session_keyring(null);
            if (keyringId < 0)
            {
                throw new InvalidOperationException("Failed to create session keyring.");
            }
        }

        private int keyId = -1;

        private const int KEYCTL_GET_KEY = 0x15;

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_join_session_keyring(void* name);

        [DllImport("libkeyutils.so")]
        private static extern int add_key(string type, string description, void* payload, uint plen, int ringid);

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_revoke(int key);

        [DllImport("libkeyutils.so")]
        private static extern int keyctl_read(int keyId, void* buffer, int size);

        private void RetrieveKeyFromKeyring(byte* key, int size, bool renew)
        {
            if (keyId == -1 || renew)
            {
                if (keyId != -1)
                {
                    var result = keyctl_revoke(keyId);
                    if (keyId < 0)
                    {
                        throw new InvalidOperationException("Failed to revoke key.");
                    }
                    keyId = -1;
                }

                RandomNumberGenerator.Fill(new Span<byte>(key, size));
                keyId = add_key("user", "aes_key", key, (uint)size, keyringId);
                if (keyId < 0)
                {
                    throw new InvalidOperationException("Failed to add key to keyring.");
                }
                return;
            }

            if (keyctl_read(keyId, key, size) < 0)
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