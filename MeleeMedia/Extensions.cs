using MeleeMedia.Video;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MeleeMediaCLI
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thp"></param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this THP thp)
        {
            return new Bitmap(new MemoryStream(thp.ToJPEG()));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static THP ToTHP(this Bitmap bmp, long compression)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                Encoder myEncoder = Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, compression);
                myEncoderParameters.Param[0] = myEncoderParameter;

                bmp.Save(stream, jpgEncoder, myEncoderParameters);
                //bmp.Save(stream, ImageFormat.Jpeg);

                return THP.FromJPEG(stream.ToArray());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
