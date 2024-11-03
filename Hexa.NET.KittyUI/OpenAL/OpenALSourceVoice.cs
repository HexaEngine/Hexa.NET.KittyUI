namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;
    using Hexa.NET.OpenAL;
    using static Hexa.NET.KittyUI.OpenAL.Helper;

    public class OpenALSourceVoice : ISourceVoice
    {
        public const int SourceSpatializeSoft = 0x1214;
        private uint source;
        private readonly IAudioStream stream;
        private int state;
        private bool disposedValue;
        private ISubmixVoice? submix;

        private float pitch = 1;
        private float gain = 1;

        internal OpenALSourceVoice(uint sourceVoice, IAudioStream audioStream)
        {
            source = sourceVoice;
            stream = audioStream;
            OpenAL.Sourcef(source, OpenAL.AL_REFERENCE_DISTANCE, 1);
            OpenAL.Sourcef(source, OpenAL.AL_MAX_DISTANCE, float.PositiveInfinity);
            OpenAL.Sourcef(source, OpenAL.AL_ROLLOFF_FACTOR, 1);
            OpenAL.Sourcef(source, OpenAL.AL_PITCH, 1);
            OpenAL.Sourcef(source, OpenAL.AL_GAIN, 1);
            OpenAL.Sourcef(source, OpenAL.AL_MIN_GAIN, 0);
            OpenAL.Sourcef(source, OpenAL.AL_MAX_GAIN, 1);
            OpenAL.Sourcef(source, OpenAL.AL_CONE_INNER_ANGLE, 360);
            OpenAL.Sourcef(source, OpenAL.AL_CONE_OUTER_ANGLE, 360);
            OpenAL.Sourcef(source, OpenAL.AL_CONE_OUTER_GAIN, 0);
            OpenAL.Source3F(source, OpenAL.AL_POSITION, 0, 0, 0);
            OpenAL.Source3F(source, OpenAL.AL_VELOCITY, 0, 0, 0);
            OpenAL.Source3F(source, OpenAL.AL_DIRECTION, 0, 0, 0);
            OpenAL.Sourcei(source, OpenAL.AL_LOOPING, 0);
            OpenAL.Sourcei(source, OpenAL.AL_SOURCE_SPATIALIZE_SOFT, 1);
            audioStream.Initialize(source);
        }

        public ISubmixVoice? Submix
        {
            get => submix;
            set
            {
                if (submix == value)
                    return;
                if (submix != null)
                {
                    submix.GainChanged -= Submix_GainChanged;
                }
                if (value != null)
                {
                    value.GainChanged += Submix_GainChanged;
                }
                submix = value;
            }
        }

        public IAudioStream Buffer => stream;

        /// <summary>
        /// Specify the pitch to be applied, either at Source, or on mixer results, at Listener.
        /// Range: [0.5f - 2.0f] Default: 1.0f.
        /// </summary>
        public float Pitch
        {
            get => pitch;
            set
            {
                pitch = value;
                OpenAL.Sourcef(source, OpenAL.AL_PITCH, value);
            }
        }

        /// <summary>
        /// Indicate the gain (volume amplification) applied. Type: float. Range: [0.0f -
        /// ? ] A value of 1.0 means un-attenuated/unchanged. Each division by 2 equals an
        /// attenuation of -6dB. Each multiplicaton with 2 equals an amplification of +6dB.
        /// A value of 0.0f is meaningless with respect to a logarithmic scale; it is interpreted
        /// as zero volume - the channel is effectively disabled.
        /// </summary>
        public float Gain
        {
            get => gain;
            set
            {
                gain = value;
                if (submix != null)
                {
                    OpenAL.Sourcef(source, OpenAL.AL_GAIN, value * submix.Gain);
                }
                else
                {
                    OpenAL.Sourcef(source, OpenAL.AL_GAIN, value);
                }
            }
        }

        public bool Looping
        {
            get => stream.Looping;
            set
            {
                stream.Looping = value;
            }
        }

        public AudioSourceState State => Convert(state);

        public event Action<AudioSourceState>? OnStateChanged;

        public event Action? OnPlay;

        public event Action? OnPause;

        public event Action? OnRewind;

        public event Action? OnStop;

        public IEmitter? Emitter { get; set; }

        public void Update()
        {
            if (Emitter != null)
            {
                OpenAL.Sourcef(source, OpenAL.AL_REFERENCE_DISTANCE, Emitter.ReferenceDistance);
                OpenAL.Sourcef(source, OpenAL.AL_MAX_DISTANCE, Emitter.MaxDistance);
                OpenAL.Sourcef(source, OpenAL.AL_ROLLOFF_FACTOR, Emitter.RolloffFactor);
                OpenAL.Sourcef(source, OpenAL.AL_MIN_GAIN, Emitter.MinGain);
                OpenAL.Sourcef(source, OpenAL.AL_MAX_GAIN, Emitter.MaxGain);
                OpenAL.Sourcef(source, OpenAL.AL_CONE_INNER_ANGLE, Emitter.ConeInnerAngle);
                OpenAL.Sourcef(source, OpenAL.AL_CONE_OUTER_ANGLE, Emitter.ConeOuterAngle);
                OpenAL.Sourcef(source, OpenAL.AL_CONE_OUTER_GAIN, Emitter.ConeOuterGain);
                var pos = Emitter.Position;
                var vel = Emitter.Velocity;
                var dir = Emitter.Direction;
                OpenAL.Source3F(source, OpenAL.AL_POSITION, pos.X, pos.Y, pos.Z);
                OpenAL.Source3F(source, OpenAL.AL_VELOCITY, vel.X, vel.Y, vel.Z);
                OpenAL.Source3F(source, OpenAL.AL_DIRECTION, dir.X, dir.Y, dir.Z);
            }
            int stateValue = 0;
            OpenAL.GetBufferi(source, OpenAL.AL_SOURCE_STATE, ref stateValue);
            var newState = stateValue;
            if (newState != state)
            {
                state = newState;
                OnStateChanged?.Invoke(Convert(state));
            }
            if (state == OpenAL.AL_PLAYING)
            {
                stream.Update(source);
            }
        }

        public void Play()
        {
            OpenAL.SourcePlay(source);
            OnPlay?.Invoke();
        }

        public void Stop()
        {
            OpenAL.SourceStop(source);
            stream.Reset();
            OnStop?.Invoke();
        }

        public void Pause()
        {
            OpenAL.SourcePause(source);
            OnPause?.Invoke();
        }

        public void Rewind()
        {
            OpenAL.SourceRewind(source);
            stream.Reset();
            OnRewind?.Invoke();
        }

        private void Submix_GainChanged(float submixGain)
        {
            OpenAL.Sourcef(source, OpenAL.AL_GAIN, gain * submixGain);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OpenAL.DeleteBuffers(1, ref source);
                disposedValue = true;
            }
        }

        ~OpenALSourceVoice()
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