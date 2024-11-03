namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.OpenAL;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    internal static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool CheckError(ALCdevice* device, [CallerFilePath] string filename = "", [CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
        {
            int error = OpenAL.GetError(device);
            if (error != OpenAL.ALC_NO_ERROR)
            {
                Debug.WriteLine($"***OpenAL ERROR*** ({filename}: {line}, {name})");
                throw error switch
                {
                    OpenAL.ALC_INVALID_VALUE => new OpenALException("ALC_INVALID_VALUE: an invalid value was passed to an OpenAL function"),
                    OpenAL.ALC_INVALID_DEVICE => new OpenALException("ALC_INVALID_DEVICE: a bad device was passed to an OpenAL function"),
                    OpenAL.ALC_INVALID_CONTEXT => new OpenALException("ALC_INVALID_CONTEXT: a bad context was passed to an OpenAL function"),
                    OpenAL.ALC_INVALID_ENUM => new OpenALException("ALC_INVALID_ENUM: an unknown enum value was passed to an OpenAL function"),
                    OpenAL.ALC_OUT_OF_MEMORY => new OpenALException("ALC_OUT_OF_MEMORY: an unknown enum value was passed to an OpenAL function"),
                    _ => new OpenALException($"UNKNOWN ALC ERROR: {error}"),
                };
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AudioSourceState Convert(int state)
        {
            return state switch
            {
                OpenAL.AL_INITIAL => AudioSourceState.Initial,
                OpenAL.AL_PLAYING => AudioSourceState.Playing,
                OpenAL.AL_PAUSED => AudioSourceState.Paused,
                OpenAL.AL_STOPPED => AudioSourceState.Stopped,
                _ => throw new NotImplementedException(),
            };
        }
    }
}