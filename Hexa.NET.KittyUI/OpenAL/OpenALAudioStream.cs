﻿namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.KittyUI.Audio;
    using System;

    public abstract class OpenALAudioStream : IAudioStream
    {
        public abstract bool Looping { get; set; }

        public abstract event Action? EndOfStream;

        public abstract void FullCommit(uint source);

        public abstract void Initialize(uint source);

        public abstract void Reset();

        public abstract void Update(uint source);
    }
}