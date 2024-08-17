namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;

    public unsafe class OpenALMasteringVoice : IMasteringVoice
    {
        private float gain = 1;

        public OpenALMasteringVoice(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public float Gain
        {
            get => gain;
            set
            {
                if (gain == value) return;
                gain = value;
                GainChanged?.Invoke(value);
            }
        }

        public event Action<float>? GainChanged;
    }
}