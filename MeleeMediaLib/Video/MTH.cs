using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MeleeMedia.Video
{
    /// <summary>
    /// 
    /// </summary>
    public class MTH
    {
        private List<THP> Frames = new List<THP>();

        public short VersionMajor { get; internal set; } = 0x08;
        public short VersionMinor { get; internal set; } = 0x00;

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int FrameRate { get; internal set; }
        public int FrameCount { get => Frames.Count; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="frameRate"></param>
        public MTH(int width, int height, int frameRate)
        {
            Width = width;
            Height = height;
            FrameRate = frameRate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mthFile"></param>
        public MTH(string mthFile)
        {
            using (var stream = new FileStream(mthFile, FileMode.Open))
            using (BinaryReader r = new BinaryReader(stream))
            {
                if (new string(r.ReadChars(4)) != "MTHP")
                    throw new InvalidDataException("MTP file is not valid");

                VersionMajor = BitConverter.ToInt16(r.ReadBytes(2).Reverse().ToArray(), 0);
                VersionMinor = BitConverter.ToInt16(r.ReadBytes(2).Reverse().ToArray(), 0);

                var sizemarker = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0); // always 2
                var maxFrameSize = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0); //  size of max frame

                Width = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                Height = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                FrameRate = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                var frameCount = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);

                var videoOff = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                var audioOff = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                var videoSize = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);
                var audioSize = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);

                r.BaseStream.Position = videoOff;

                var max = videoSize;
                for (int f = 0; f < frameCount; f++)
                {
                    var nextVideoSize = BitConverter.ToInt32(r.ReadBytes(4).Reverse().ToArray(), 0);

                    Frames.Add(new THP(r.ReadBytes(videoSize - 4)));

                    videoSize = nextVideoSize;
                    max = Math.Max(max, videoSize);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Save(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(stream))
            {
                w.Write("MTHP".ToCharArray());

                w.Write(BitConverter.GetBytes(VersionMajor).Reverse().ToArray());
                w.Write(BitConverter.GetBytes(VersionMinor).Reverse().ToArray());

                w.Write(BitConverter.GetBytes(2).Reverse().ToArray());
                w.Write(0); // max frame size

                w.Write(BitConverter.GetBytes(Width).Reverse().ToArray());
                w.Write(BitConverter.GetBytes(Height).Reverse().ToArray());
                w.Write(BitConverter.GetBytes(FrameRate).Reverse().ToArray());
                w.Write(BitConverter.GetBytes(FrameCount).Reverse().ToArray());

                w.Write(BitConverter.GetBytes(0x40).Reverse().ToArray()); // video off
                w.Write(0); // audio off (unused)
                var first_frame_size = Frames.Count > 0 ? Frames[0].Data.Length + 4 : 0;
                first_frame_size += 0x20 - (first_frame_size % 0x20);
                w.Write(BitConverter.GetBytes(first_frame_size).Reverse().ToArray()); // video start size
                w.Write(0); // audio start size (unused)

                // other channels (unused)
                w.Write(0);
                w.Write(0);
                w.Write(0);
                w.Write(0);

                var max = Frames.Count > 0 ? Frames[0].Data.Length : 0;
                for (int f = 0; f < FrameCount; f++)
                {
                    var nextVideoSize = 0;
                    if (f + 1 < FrameCount)
                    {
                        nextVideoSize = Frames[f + 1].Data.Length + 4;
                        nextVideoSize += 0x20 - (nextVideoSize % 0x20);
                    }
                    else
                    {
                        nextVideoSize = Frames[0].Data.Length + 4;
                        nextVideoSize += 0x20 - (nextVideoSize % 0x20);
                    }

                    w.Write(BitConverter.GetBytes(nextVideoSize).Reverse().ToArray());
                    w.Write(Frames[f].Data);
                    var padding = 0x20 - ((Frames[f].Data.Length + 4) % 0x20);
                    w.Write(new byte[padding]);

                    max = Math.Max(max, Frames[f].Data.Length + 4 + padding);
                }

                w.BaseStream.Position = 0x0C;
                w.Write(BitConverter.GetBytes(max).Reverse().ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public THP GetFrame(int frame)
        {
            if (frame < 0 || frame > Frames.Count)
                throw new IndexOutOfRangeException();

            return Frames[frame];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        public void AddFrame(Bitmap bmp, long compression)
        {
            Frames.Add(THP.FromBitmap(bmp, compression));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        public void RemoveFrame(int index)
        {
            Frames.RemoveAt(index);
        }

    }
}
