﻿namespace Hexa.NET.KittyUI.Audio
{
    using System;

    public interface IMasteringVoice
    {
        float Gain { get; set; }
        string Name { get; }

        event Action<float>? GainChanged;
    }
}