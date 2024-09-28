using System;
using System.IO;
using System.Linq;

namespace MeleeMedia.Video
{
    public class MTHWriter : IDisposable
    {
        private readonly Stream _stream;
        private int _frameCount = 0;
        private int _firstFrameSize = 0;
        private int _maxFrameSize = 0;
        private long _previousFrameStart = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="frameRate"></param>
        public MTHWriter(Stream stream, int width, int height, int frameRate)
        {
            _stream = stream;

            // write header
            WriteChars("MTHP".ToCharArray());

            WriteInt16(8); // version major
            WriteInt16(0); // version minor

            WriteInt32(2); // size marker
            WriteInt32(0); // max frame size

            WriteInt32(width);
            WriteInt32(height);
            WriteInt32(frameRate);
            WriteInt32(0); // frame count

            // offsets and data
            WriteInt32(0x40); // video offset
            WriteInt32(0); // audio start offset
            _previousFrameStart = _stream.Position;
            WriteInt32(0); // first frame size
            WriteInt32(0); // audio start size

            // other channels (unused)
            WriteInt32(0);
            WriteInt32(0);
            WriteInt32(0);
            WriteInt32(0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        public void WriteFrame(THP frame)
        {
            // get thp image data
            var data = frame.Data;

            // calculate aligned data length
            var data_length = data.Length + 4;
            if (data_length % 0x20 != 0)
                data_length += 0x20 - (data_length % 0x20);

            // track max frame size
            _maxFrameSize = Math.Max(_maxFrameSize, data_length);

            // begin writing
            if (_frameCount == 0)
            {
                _firstFrameSize = data_length;
            }

            // update previous frame lookup buffer
            if (_previousFrameStart != 0)
            {
                var temp = _stream.Position;
                _stream.Position = _previousFrameStart;
                WriteInt32(data_length);
                _stream.Position = temp;
            }

            // write data chunk
            _previousFrameStart = _stream.Position;
            WriteInt32(0);

            // write data
            _stream.Write(data, 0, data.Length);

            // write padding
            var padding_size = data_length - (data.Length + 4);
            _stream.Write(new byte[padding_size], 0, padding_size);

            _frameCount++;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        private void WriteChars(char[] chars)
        {
            _stream.Write(chars.Select(e => (byte)e).ToArray(), 0, chars.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        private void WriteInt16(short v)
        {
            _stream.Write(BitConverter.GetBytes(v).Reverse().ToArray(), 0, 2);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        private void WriteInt32(int v)
        {
            _stream.Write(BitConverter.GetBytes(v).Reverse().ToArray(), 0, 4);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // update last frame by setting to first frame size
            if (_previousFrameStart != 0)
            {
                _stream.Position = _previousFrameStart;
                WriteInt32(_firstFrameSize);
            }

            // write maxframesize
            _stream.Position = 0x0C;
            WriteInt32(_maxFrameSize);

            // write framecount
            _stream.Position = 0x1C;
            WriteInt32(_frameCount);

            // close and dispose of streams
            _stream.Close();
            _stream.Dispose();
        }
    }
}
