using AForge.Video.FFMPEG;
using MeleeMedia.Audio;
using MeleeMedia.Video;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MeleeMediaCLI.Video
{
    public class VideoConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thp"></param>
        /// <param name="filePath"></param>
        private static void ExportTHPFrame(THP thp, string filePath)
        {
            var ext = System.IO.Path.GetExtension(filePath).ToLower();
            ImageFormat fmt = ImageFormat.Jpeg;

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    System.IO.File.WriteAllBytes(filePath, thp.ToJPEG());
                    return;
                case ".bmp":
                    fmt = ImageFormat.Bmp;
                    break;
                case ".png":
                    fmt = ImageFormat.Png;
                    break;
            }

            using (var bmpFrame = thp.ToBitmap())
            {
                bmpFrame.Save(filePath, fmt);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mth"></param>
        /// <param name="filePath"></param>
        public static bool MTHtoImages(MTH mth, string filepath, string extension)
        {
            // write all frames
            var numberFormat = $"D{Math.Floor(Math.Log10(mth.FrameCount) + 1)}";
            for (int i = 0; i < mth.FrameCount; i++)
            {
                var frame = mth.GetFrame(i);
                ExportTHPFrame(frame, $"{filepath}{i.ToString(numberFormat)}.{extension}");
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MTH ImagestoMTH(string filePath, int frameWidth, int frameHeight)
        {
            // TODO:
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mth"></param>
        /// <param name="filePath"></param>
        public static void MTHtoMP4(string mthPath, string mp4Path)
        {
            using (FileStream fstream = new FileStream(mthPath, FileMode.Create))
            using (var mth = new MTHReader(fstream))
            using (var vFWriter = new VideoFileWriter())
            {
                // create new video file
                vFWriter.Open(mp4Path, mth.Width, mth.Height, mth.FrameRate, VideoCodec.MPEG4);

                // write all frames
                for (int i = 0; i < mth.FrameCount; i++)
                    using (var bmpFrame = mth.ReadFrame().ToBitmap())
                        vFWriter.WriteVideoFrame(bmpFrame);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static void MP4toMTH(string mp4Path, string mthPath, int frameWidth, int frameHeight, long compression)
        {
            using (FileStream fstream = new FileStream(mthPath, FileMode.Create))
            using (VideoFileReader reader = new VideoFileReader())
            {
                // open video file
                reader.Open(mp4Path);

                if (frameWidth == -1)
                    frameWidth = reader.Width;

                if (frameHeight == -1)
                    frameHeight = reader.Height;

                using (MTHWriter mth = new MTHWriter(fstream, frameWidth, frameHeight, reader.FrameRate))
                {
                    // use original video width and height
                    if (frameHeight == -1)
                        frameHeight = reader.Height;

                    if (frameWidth == -1)
                        frameWidth = reader.Width;

                    // calculate frame rate
                    //var rate = reader.FrameRate / (float)frameRate;

                    // copy frames
                    float curr_frame = 0;
                    while (curr_frame < reader.FrameCount)
                    {
                        //var dis = (int)((curr_frame + rate) - curr_frame) - 1;
                        curr_frame += 1;

                        using (Bitmap frame = reader.ReadVideoFrame())
                        {
                            var thp = frame.ToTHP(compression);
                            if (frame.Width != frameWidth || frame.Height != frameHeight)
                            {
                                using (var resize = ResizeBitmap(frame, frameWidth, frameHeight))
                                {
                                    mth.WriteFrame(thp);
                                }
                            }
                            else
                            {
                                mth.WriteFrame(thp);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(bmp, 0, 0, width, height);
            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="filepath"></param>
        /// <param name="extension"></param>
        public static void THPtoImages(string filePath, string filepath, string extension)
        {
            using (var thp = new THPVideoReader(filePath))
            {
                WAVE wav = new WAVE()
                {
                    Frequency = (int)thp.AudioFrequency,
                    BitsPerSample = 16,
                };

                // write all frames
                var numberFormat = $"D{Math.Floor(Math.Log10(thp.FrameCount) + 1)}";
                for (int i = 0; i < thp.FrameCount; i++)
                {
                    thp.ReadFrame(out THP frame, out WAVE audioFrame);

                    if (audioFrame != null)
                    {
                        wav.Append(audioFrame);
                    }

                    ExportTHPFrame(frame, $"{filepath}{i.ToString(numberFormat)}.{extension}");
                }
                wav.Write($"{filepath}.wav");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thpPath"></param>
        /// <param name="mp4Path"></param>
        public static void THPtoMP4(string thpPath, string mp4Path)
        {
            using (var thp = new THPVideoReader(thpPath))
            using (var vFWriter = new VideoFileWriter())
            {
                // create new video file
                vFWriter.Open(mp4Path, (int)thp.FrameWidth, (int)thp.FrameHeight, (int)Math.Ceiling(thp.FrameRate), VideoCodec.MPEG4);

                // write all frames
                WAVE wav = new WAVE()
                {
                    Frequency = (int)thp.AudioFrequency,
                    BitsPerSample = 16,
                };
                for (int i = 0; i < thp.FrameCount; i++)
                {
                    thp.ReadFrame(out THP frame, out WAVE audioFrame);
                    if (audioFrame != null)
                    {
                        wav.Append(audioFrame);
                    }

                    using (var bmpFrame = frame.ToBitmap())
                        vFWriter.WriteVideoFrame(bmpFrame);
                }
                wav.Write(mp4Path.Replace(".mp4", ".wav"));
            }
        }
    }
}
