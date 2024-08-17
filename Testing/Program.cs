namespace Testing
{
    using Hexa.NET.KittyUI.Security;
    using System.Diagnostics;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    internal unsafe class Program
    {
        private static void Main(string[] args)
        {
            long now = Stopwatch.GetTimestamp();
            Thread.Sleep(1);
            long elapsed = Stopwatch.GetTimestamp() - now;
            double elapsedMs = (double)elapsed / Stopwatch.Frequency * 1000;
        }

        private static void LinuxMemTest()
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
    }
}