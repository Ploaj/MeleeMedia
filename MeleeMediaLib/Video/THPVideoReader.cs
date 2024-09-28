using MeleeMedia.Audio;
using System;
using System.IO;

namespace MeleeMedia.Video
{
    public class THPVideoReader : IDisposable
    {
        private Stream _stream;

        public float FrameRate { get; internal set; }

        public uint FrameCount { get; internal set; }

        private class VideoComponent
        {
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint Format { get; set; }
        }

        private class AudioComponent
        {
            public uint NumChannels { get; set; }
            public uint Frequency { get; set; }
            public uint NumSamples { get; set; }
            public uint NumData { get; set; }
        }

        private readonly VideoComponent _videoComponent;
        private readonly AudioComponent _audioComponent;

        private uint FirstFrameOffset { get; set; }
        private uint LastFrameOffset { get; set; }

        private uint CurrentFrameSize { get; set; }
        public int Frame { get; internal set; }

        public uint FrameWidth { get => _videoComponent.Width; }
        public uint FrameHeight { get => _videoComponent.Height; }
        public uint AudioFrequency { get => _audioComponent.Frequency; }
        public uint AudioChannelCount { get => _audioComponent.NumChannels; }

        public THPVideoReader(Stream s)
        {
            _stream = s;

            if (ReadUInt32() != 0x54485000)
                throw new InvalidDataException("THP Format is not valid");

            if (ReadUInt32() != 0x11000)
                throw new InvalidDataException("Unsupported THP Version");

            ReadUInt32(); // maxBufferSize
            ReadUInt32(); // maxAudioSamples
            FrameRate = ReadSingle();
            FrameCount = ReadUInt32();
            CurrentFrameSize = ReadUInt32(); // length of first frame
            ReadUInt32(); // length of all frames
            var componentOffset = ReadUInt32();
            if (ReadUInt32() != 0)
                throw new InvalidDataException("Unsupported THP Version");
            FirstFrameOffset = ReadUInt32();
            LastFrameOffset = ReadUInt32();

            s.Position = componentOffset;
            var componentCount = ReadUInt32();

            var components = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                components[i] = (byte)s.ReadByte();
            }

            for (int i = 0; i < componentCount; i++)
            {
                switch (components[i])
                {
                    case 0:
                        {
                            if (_videoComponent == null)
                            {
                                _videoComponent = new VideoComponent()
                                {
                                    Width = ReadUInt32(),
                                    Height = ReadUInt32(),
                                    Format = ReadUInt32(),
                                };
                            }
                            else
                            {
                                throw new NotSupportedException("Unsupported multiple video components");
                            }
                        }
                        break;
                    case 1:
                        {
                            if (_audioComponent == null)
                            {
                                _audioComponent = new AudioComponent()
                                {
                                    NumChannels = ReadUInt32(),
                                    Frequency = ReadUInt32(),
                                    NumSamples = ReadUInt32(),
                                    NumData = ReadUInt32(),
                                };
                            }
                            else
                            {
                                throw new NotSupportedException("Unsupported multiple audio components");
                            }
                        }
                        break;
                    case 255:
                        break;
                    default:
                        throw new NotSupportedException("Unknown component type " + components[i]);
                }
            }

            // seek start
            s.Position = FirstFrameOffset;
            Frame = 0;
        }

        public THPVideoReader(string filePath) : this(new FileStream(filePath, FileMode.Open))
        {

        }

        public void ReadFrame(out THP thp, out WAVE wav)
        {
            var nextFramePosition = _stream.Position + CurrentFrameSize;

            // header
            CurrentFrameSize = ReadUInt32();
            ReadUInt32(); // previousSize
            var imageSize = ReadUInt32();
            var audioSize = ReadUInt32();

            // video data
            thp = new THP(ReadBytes(imageSize));

            // audio data
            wav = null;
            if (audioSize > 0 && _audioComponent != null)
            {
                wav = new WAVE()
                {
                    Frequency = (int)_audioComponent.Frequency,
                    BitsPerSample = 16,
                };

                var channelSize = ReadUInt32();
                ReadUInt32(); // sampleCount
                short[] coef1 = ReadCoefs();
                short[] coef2 = ReadCoefs();
                short history1 = ReadInt16();
                short history2 = ReadInt16();
                short history3 = ReadInt16();
                short history4 = ReadInt16();

                for (int i = 0; i < _audioComponent.NumChannels; i++)
                {
                    var encoded = ReadBytes(channelSize);
                    var decoded = GcAdpcmDecoder.Decode(
                        encoded, 
                        i == 0 ? coef1 : coef2,
                        i == 0 ? history1 : history3,
                        i == 0 ? history2 : history4);
                    wav.Channels.Add(decoded);
                }
            }

            // move to next frame
            _stream.Position = nextFramePosition;
        }

        private byte[] ReadBytes(uint length)
        {
            var b = new byte[length];
            for (int i = 0; i < length; i++)
                b[i] = (byte)_stream.ReadByte();
            return b;
        }

        private short[] ReadCoefs()
        {
            var b = new short[16];
            for (int i = 0; i < 16; i++)
                b[i] = ReadInt16();
            return b;
        }

        private uint ReadUInt32()
        {
            return (uint)(((_stream.ReadByte() & 0xFF) << 24) | ((_stream.ReadByte() & 0xFF) << 16) | ((_stream.ReadByte() & 0xFF) << 8) | (_stream.ReadByte() & 0xFF));
        }

        private short ReadInt16()
        {
            return (short)(((_stream.ReadByte() & 0xFF) << 8) | (_stream.ReadByte() & 0xFF));
        }

        private float ReadSingle()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt32()), 0);
        }

        public void Dispose()
        {
            _stream.Close();
            _stream.Dispose();
            _stream = null;
        }
    }
}
