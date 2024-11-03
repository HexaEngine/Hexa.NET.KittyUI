namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.OpenAL;
    using Hexa.NET.Utilities;

    public unsafe class OpenALAudioContext : IAudioContext
    {
        internal readonly OpenALAudioDevice AudioDevice;

        [SuppressFreeWarning]
        internal readonly ALCdevice* Device;

        [SuppressFreeWarning]
        public readonly ALCcontext* Context;

        private bool disposedValue;

        public nint NativePointer => (nint)Context;

        internal OpenALAudioContext(OpenALAudioDevice audioDevice, ALCcontext* context)
        {
            AudioDevice = audioDevice;
            Device = audioDevice.Device;
            Context = context;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OpenAL.DestroyContext(Context);
                disposedValue = true;
            }
        }

        ~OpenALAudioContext()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}