namespace Hexa.NET.KittyUI.Audio
{
    using System;

    public interface ISubmixVoice
    {
        float Gain { get; set; }

        IMasteringVoice Master { get; }

        string Name { get; set; }

        event Action<float>? GainChanged;
    }
}