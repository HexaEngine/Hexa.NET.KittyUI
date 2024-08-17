﻿namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.Utilities;
    using Silk.NET.OpenAL;
    using static Hexa.NET.KittyUI.OpenAL.Helper;

    public unsafe class OpenALAudioContext : IAudioContext
    {
        internal readonly OpenALAudioDevice AudioDevice;

        [SuppressFreeWarning]
        internal readonly Device* Device;

        [SuppressFreeWarning]
        public readonly Context* Context;

        private bool disposedValue;

        public nint NativePointer => (nint)Context;

        internal OpenALAudioContext(OpenALAudioDevice audioDevice, Context* context)
        {
            AudioDevice = audioDevice;
            Device = audioDevice.Device;
            Context = context;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                alc.DestroyContext(Context);
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