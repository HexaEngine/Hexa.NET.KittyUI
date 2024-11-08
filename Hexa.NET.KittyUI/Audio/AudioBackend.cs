namespace Hexa.NET.KittyUI.Audio
{
    public enum AudioBackend
    {
        /// <summary>
        /// Indicates that the audio-sub system is disabled.
        /// </summary>
        Disabled = -1,
        
        /// <summary>
        /// Automatically selects audio backend.
        /// </summary>
        Auto,
        
        /// <summary>
        /// OpenAL audio backend.
        /// </summary>
        OpenAL,
        
        /// <summary>
        /// Not supported at the moment.
        /// </summary>
        XAudio2,
    }
}