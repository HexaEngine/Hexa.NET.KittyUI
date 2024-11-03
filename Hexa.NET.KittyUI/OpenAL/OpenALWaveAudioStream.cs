namespace Hexa.NET.KittyUI.OpenAL
{
    using Hexa.NET.OpenAL;

    public unsafe class OpenALWaveAudioStream : OpenALAudioStream
    {
        private readonly Stream stream;
        private readonly uint* buffers;
        private readonly int bufferCount;
        private readonly int bufferSize;
        private readonly byte[] buffer;
        public readonly uint SampleOffset;
        public readonly uint ByteOffset;
        public readonly int Type;
        public readonly WaveHeader Header;
        public readonly int Format;
        private int position;
        private bool looping;
        private bool reachedEnd;

        public OpenALWaveAudioStream(Stream stream, int bufferCount = 4, int bufferSize = 65536)
        {
            Type = OpenAL.AL_STREAMING;
            Header = new(stream);
            Format = Header.GetBufferFormat();
            if (Header.AudioFormat != WaveFormatEncoding.Pcm) throw new NotSupportedException("Wav PCM only");
            this.stream = stream;
            this.bufferCount = bufferCount;
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
            buffers = AllocT<uint>(bufferCount);
            OpenAL.GenBuffers(bufferCount, buffers);
        }

        public int Position => position;

        public bool ReachedEnd => reachedEnd;

        public override bool Looping { get => looping; set => looping = value; }

        public override event Action? EndOfStream;

        public override void Reset()
        {
            reachedEnd = false;
            position = 0;
        }

        public override void FullCommit(uint source)
        {
            stream.Position = Header.DataBegin;
            var data = stream.Read(Header.DataSize);
            fixed (byte* buffer = data)
                OpenAL.BufferData(buffers[0], (int)Format, buffer, Header.DataSize, Header.SampleRate);
            OpenAL.Sourcei(source, (int)OpenAL.AL_BUFFER, (int)buffers[0]);
        }

        public override void Initialize(uint source)
        {
            for (int i = 0; i < bufferCount; i++)
            {
                if (reachedEnd) return;
                var absPosition = Header.DataBegin + position;

                long dataSizeToCopy = bufferSize;
                if (absPosition + bufferSize > stream.Length)
                    dataSizeToCopy = stream.Length - absPosition;
                stream.Position = absPosition;
                stream.Read(buffer, 0, (int)dataSizeToCopy);
                position += (int)dataSizeToCopy;

                if (dataSizeToCopy < bufferSize)
                {
                    if (!looping)
                    {
                        position = 0;
                        EndOfStream?.Invoke();
                        reachedEnd = true;
                        fixed (byte* pData = buffer)
                        {
                            OpenAL.BufferData(buffers[i], (int)Format, pData, (int)dataSizeToCopy, Header.SampleRate);
                        }
                        return;
                    }
                    stream.Position = Header.DataBegin;
                    stream.Read(buffer, (int)dataSizeToCopy, (int)(bufferSize - dataSizeToCopy));
                    position = (int)(bufferSize - dataSizeToCopy);
                }

                fixed (byte* pData = buffer)
                {
                    OpenAL.BufferData(buffers[i], (int)Format, pData, bufferSize, Header.SampleRate);
                }
                OpenAL.SourceQueueBuffers(source, 1, &buffers[i]);
            }
        }

        public override void Update(uint source)
        {
            if (reachedEnd) return;
            int buffersProcessed = 0;
            OpenAL.GetBufferi(source, (int)OpenAL.AL_BUFFERS_PROCESSED, ref buffersProcessed);
            if (buffersProcessed <= 0)
                return;
            while (buffersProcessed-- != 0)
            {
                uint bufferId;
                OpenAL.SourceUnqueueBuffers(source, 1, &bufferId);

                var absPosition = Header.DataBegin + position;

                long dataSizeToCopy = bufferSize;
                if (absPosition + bufferSize > stream.Length)
                    dataSizeToCopy = stream.Length - absPosition;
                stream.Position = absPosition;
                stream.Read(buffer, 0, (int)dataSizeToCopy);
                position += (int)dataSizeToCopy;

                if (dataSizeToCopy < bufferSize)
                {
                    if (!looping)
                    {
                        position = 0;
                        EndOfStream?.Invoke();
                        reachedEnd = true;
                        fixed (byte* pData = buffer)
                        {
                            OpenAL.BufferData(bufferId, (int)Format, pData, (int)dataSizeToCopy, Header.SampleRate);
                        }
                        return;
                    }
                    stream.Position = Header.DataBegin;
                    stream.Read(buffer, (int)dataSizeToCopy, (int)(bufferSize - dataSizeToCopy));
                    position = (int)(bufferSize - dataSizeToCopy);
                }

                fixed (byte* pData = buffer)
                {
                    OpenAL.BufferData(bufferId, (int)Format, pData, bufferSize, Header.SampleRate);
                }
                OpenAL.SourceQueueBuffers(source, 1, &bufferId);
            }
        }
    }
}