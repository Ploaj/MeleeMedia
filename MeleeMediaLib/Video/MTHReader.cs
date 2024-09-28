using System;
using System.IO;
using System.Threading.Tasks;

namespace MeleeMedia.Video
{
    public class MTHReader : IDisposable
    {
        public short VersionMajor { get; internal set; }
        public short VersionMinor { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FrameRate { get; internal set; }

        public int FrameCount { get; internal set; }

        private readonly Stream _stream;

        private readonly int _frameStart;
        private readonly int _frameStartSize;
        private int _frameSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private short ReadInt16(Stream s)
        {
            return (short)(((s.ReadByte() & 0xFF) << 8) | (s.ReadByte() & 0xFF));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private int ReadInt32(Stream s)
        {
            return (int)(((s.ReadByte() & 0xFF) << 24) | ((s.ReadByte() & 0xFF) << 16) | ((s.ReadByte() & 0xFF) << 8) | (s.ReadByte() & 0xFF));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <exception cref="InvalidDataException"></exception>
        public MTHReader(Stream r)
        {
            r.Position = 0;

            if (ReadInt32(r) != 0x4D544850)
                throw new InvalidDataException("MTP file is not valid");

            VersionMajor = ReadInt16(r);
            VersionMinor = ReadInt16(r);

            ReadInt32(r); // sizemarker always 2
            ReadInt32(r); // maxFrameSize size of max frame

            Width = ReadInt32(r);
            Height = ReadInt32(r);
            FrameRate = ReadInt32(r);
            FrameCount = ReadInt32(r);

            _frameStart = ReadInt32(r);
            ReadInt32(r); // audioOff 
            _frameSize = ReadInt32(r);
            ReadInt32(r); // audioSize

            _frameStartSize = _frameSize;
            r.Position = _frameStart;
            _stream = r;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        public void Seek(int frame)
        {
            if (frame < 0 || frame >= FrameCount)
                return;

            // start from beginning
            _frameSize = _frameStartSize;
            _stream.Position = _frameStart;

            // seek frame
            for (int i = 0; i < frame; i++)
            {
                var nextVideoSize = ReadInt32(_stream);
                _stream.Position += _frameSize - 4;
                _frameSize = nextVideoSize;
                if (_frameSize == 0)
                    _frameSize = _frameStartSize;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public THP ReadFrame()
        {
            var nextVideoSize = ReadInt32(_stream);
            byte[] data = new byte[_frameSize - 4];
            _stream.Read(data, 0, data.Length);
            _frameSize = nextVideoSize;
            if (_frameSize == 0)
                _frameSize = _frameStartSize;

            if (_stream.Position >= _stream.Length)
                _stream.Position = _frameStart;

            return new THP(data);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<THP> ReadFrameAsync()
        {
            var nextVideoSize = ReadInt32(_stream);
            byte[] data = new byte[_frameSize - 4];
            await _stream.ReadAsync(data, 0, data.Length);
            _frameSize = nextVideoSize;
            if (_frameSize == 0)
                _frameSize = _frameStartSize;

            if (_stream.Position >= _stream.Length)
                _stream.Position = _frameStart;

            return new THP(data);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
