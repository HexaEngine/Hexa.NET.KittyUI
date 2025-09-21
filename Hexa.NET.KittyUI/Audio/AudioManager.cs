namespace Hexa.NET.KittyUI.Audio
{
    public static class AudioManager
    {
#nullable disable
        private static IAudioContext audioContext;
        private static IAudioDevice audioDevice;
        private static IMasteringVoice master;
        private static Thread streamThread;
#nullable enable
        private static bool running;

        public static AudioBackend Backend => audioDevice?.Backend ?? AudioBackend.Disabled;

        public static IAudioDevice Device => audioDevice;

        public static IAudioContext Context => audioContext;

        public static IMasteringVoice Master => master;

        public static void Initialize(IAudioDevice device)
        {
            audioDevice = device;
            audioContext = audioDevice.Default;
            master = audioDevice.CreateMasteringVoice("Master");
            running = true;
            streamThread = new(ThreadVoid);
            streamThread.Name = "Audio Stream Thread";
            streamThread.Start();
        }

        public static IAudioStream CreateStream(string path)
        {
            return audioDevice.CreateWaveAudioStream(File.OpenRead(path));
        }

        public static ISourceVoice CreateSourceVoice(IAudioStream stream)
        {
            return audioDevice.CreateSourceVoice(stream);
        }

        public static IListener CreateListener()
        {
            return audioDevice.CreateListener(master);
        }

        public static IEmitter CreateEmitter()
        {
            return audioDevice.CreateEmitter();
        }

        private static void ThreadVoid()
        {
            try
            {
                while (running)
                {
                    try
                    {
                        audioDevice?.ProcessAudio();
                        Thread.Sleep(10);
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Log error but continue processing
                        Thread.Sleep(100); // Wait longer on error
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                // Thread was interrupted, exit gracefully
            }
        }

        public static void Dispose()
        {
            if (audioDevice == null) return;
            
            running = false;
            
            if (streamThread != null)
            {
                streamThread.Join(5000); // Wait max 5 seconds for thread to exit
                if (streamThread.IsAlive)
                {
                    streamThread.Interrupt(); // Force interrupt if still running
                }
            }
            
            audioContext?.Dispose();
            audioDevice?.Dispose();
        }
    }
}