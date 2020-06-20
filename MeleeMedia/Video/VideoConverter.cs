using AForge.Video.FFMPEG;
using System.Drawing;

namespace MeleeMedia.Video
{
    public class VideoConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mth"></param>
        /// <param name="filePath"></param>
        public static void MTHtoMP4(MTH mth, string filePath)
        {
            using (var vFWriter = new VideoFileWriter())
            {
                // create new video file
                vFWriter.Open(filePath, mth.Width, mth.Height, mth.FrameRate, VideoCodec.MPEG4);

                // write all frames
                for (int i = 0; i < mth.FrameCount; i++)
                    using (var bmpFrame = mth.GetFrame(i).ToBitmap())
                        vFWriter.WriteVideoFrame(bmpFrame);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static MTH MP4toMTH(string filePath)
        {
            MTH mth = null;

            using (VideoFileReader reader = new VideoFileReader())
            {
                // open video file
                reader.Open(filePath);

                // create new mth container
                mth = new MTH(reader.Width, reader.Height, reader.FrameRate);

                // copy frames
                for (int i = 0; i < reader.FrameCount; i++)
                    using (Bitmap frame = reader.ReadVideoFrame())
                        mth.AddFrame(frame);
            }

            return mth;
        }
    }
}
