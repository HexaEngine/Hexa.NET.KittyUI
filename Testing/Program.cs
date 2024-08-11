namespace Testing
{
    using Hexa.NET.Kitty.Security;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    public struct ManualSemaphore
    {
        private int currentCount;
        private readonly int maxCount;
        private int spinLock;

        public ManualSemaphore(int initialCount, int maxCount)
        {
            if (initialCount > maxCount || initialCount < 0 || maxCount <= 0)
            {
                throw new ArgumentException("Invalid semaphore initial or max count");
            }

            this.currentCount = initialCount;
            this.maxCount = maxCount;
            this.spinLock = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AcquireLock()
        {
            while (Interlocked.CompareExchange(ref spinLock, 1, 0) != 0)
            {
                Thread.Yield(); // Minimiert CPU-Belastung, aber ist noch kein Spin-Waiting
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseLock()
        {
            Volatile.Write(ref spinLock, 0);
        }

        public void Wait()
        {
            while (true)
            {
                AcquireLock();
                if (currentCount > 0)
                {
                    currentCount--;
                    ReleaseLock();
                    break;
                }
                ReleaseLock();
                Thread.Sleep(1); // Minimale Blockierung, um CPU zu entlasten, kein Spin-Waiting
            }
        }

        public void Release()
        {
            AcquireLock();
            if (currentCount < maxCount)
            {
                currentCount++;
            }
            else
            {
                throw new InvalidOperationException("Semaphore released too many times");
            }
            ReleaseLock();
        }
    }

    internal unsafe class Program
    {
        static ManualSemaphore semaphore = new ManualSemaphore(2, 2);

        private static void Main(string[] args)
        {
            SecureMemoryLinux memory = new("Test");
            byte* data = stackalloc byte[16];
            Encoding.UTF8.GetBytes("Hello, World!", new Span<byte>(data, 16));

            try
            {
                memory.StoreSensitiveData(data, 16);
            }
            catch (Exception)
            {
                new Span<byte>(data, 16).Clear();
            }

            byte* output = stackalloc byte[16];
            memory.AccessSensitiveData(output, 16);

            for (int i = 0; i < 16; i++)
            {
                if (data[i] != output[i])
                {
                    throw new InvalidOperationException("Data mismatch");
                }
            }

            Console.WriteLine(Encoding.UTF8.GetString(new Span<byte>(output, 16)));
        }

        private static void Worker(int id)
        {
            Console.WriteLine($"Thread {id} is waiting to enter the semaphore");
            semaphore.Wait();
            Console.WriteLine($"Thread {id} has entered the semaphore");

            Thread.Sleep(2000); // Simuliert Arbeit

            Console.WriteLine($"Thread {id} is leaving the semaphore");
            semaphore.Release();
        }
    }
}