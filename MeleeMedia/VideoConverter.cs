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
        public static MTH MP4toMTH(string filePath, int frameWidth, int frameHeight, int frameRate)
        {
            MTH mth = null;

            using (VideoFileReader reader = new VideoFileReader())
            {
                // open video file
                reader.Open(filePath);

                // create new mth container
                mth = new MTH(frameWidth, frameHeight, frameRate);

                // calculate frame rate
                var rate = reader.FrameRate / (float)frameRate;

                // copy frames
                float curr_frame = 0;
                while (curr_frame < reader.FrameCount)
                {
                    var dis = (int)((curr_frame + rate) - curr_frame) - 1;
                    curr_frame += 1;

                    // System.Console.WriteLine(curr_frame  + " " + reader.FrameCount + " " + dis);

                    using (Bitmap frame = reader.ReadVideoFrame())
                    using (var resize = ResizeBitmap(frame, frameWidth, frameHeight))
                    {
                        mth.AddFrame(resize);
                    }

                    //for (int j = 0; j < dis; j++)
                    //    reader.ReadVideoFrame();
                }
            }

            return mth;
        }

        private static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(bmp, 0, 0, width, height);
            return result;
        }
    }
}
