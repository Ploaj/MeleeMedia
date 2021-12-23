using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MeleeMedia.Video
{
    /// <summary>
    /// 
    /// </summary>
    public class THP
    {
        public byte[] Data { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public THP(byte[] data)
        {
            Data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Save(string filePath)
        {
            File.WriteAllBytes(filePath, Data);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static THP FromBitmap(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                Encoder myEncoder = Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 25L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                bmp.Save(stream, jpgEncoder, myEncoderParameters);
                //bmp.Save(stream, ImageFormat.Jpeg);

                return FromJPEG(stream.ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jpeg"></param>
        public static THP FromJPEG(byte[] Data)
        {
            return new THP(JPEGCONV(Data, true));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Bitmap ToBitmap()
        {
            Bitmap bmp;
            {
                bmp = new Bitmap(new MemoryStream(ToJPEG()));
            }
            return bmp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToJPEG()
        {
            return JPEGCONV(Data, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        private static byte[] JPEGCONV(byte[] data, bool encode)
        {
            List<byte> frameConv = new List<byte>();

            for (int i = 0; i < data.Length;)
            {
                var code = data[i++];
                var pay = data[i++];

                if (code != 0xFF)
                {
                    throw new InvalidDataException("JPEG is not valid");
                }

                var length = 0;

                if (pay == 0xDA)
                {
                    frameConv.Add(code);
                    frameConv.Add(pay);

                    // process until 0xFFD9 is found?
                    int j;
                    for (j = i; j < data.Length; j++)
                    {
                        var end = data[j] == 0xFF && data[j] == 0xD9;

                        if (!end)
                        {
                            frameConv.Add(data[j]);
                            if (data[j] == 0xFF)
                                if (encode)
                                    j++;
                                else
                                    frameConv.Add(0x00);
                        }
                        else
                        {
                            frameConv.Add(0xFF);
                            frameConv.Add(0xD9);
                            break;
                        }
                    }
                    i = j;

                    continue;
                }
                else
                if (pay == 0xDD)
                    length = 4;
                else
                if (pay == 0xD0 ||
                    pay == 0xD1 ||
                    pay == 0xD2 ||
                    pay == 0xD3 ||
                    pay == 0xD4 ||
                    pay == 0xD5 ||
                    pay == 0xD6 ||
                    pay == 0xD7 ||
                    pay == 0xD8 ||
                    pay == 0xD9)
                    length = 0;
                else
                {
                    length = ((data[i] & 0xFF) << 8) | (data[i + 1] & 0xFF);
                }

                List<byte> newPayLoad = new List<byte>();
                for (int j = 0; j < length; j++)
                {
                    newPayLoad.Add(data[i + j]);
                    if (data[i + j] == 0xFF)
                        if (encode)
                            j++;
                        else
                            frameConv.Add(0x00);
                }

                frameConv.Add(code);
                frameConv.Add(pay);
                if (newPayLoad.Count > 0)
                {
                    newPayLoad[0] = (byte)(newPayLoad.Count >> 8);
                    newPayLoad[1] = (byte)(newPayLoad.Count & 0xFF);
                }
                frameConv.AddRange(newPayLoad);
                i += length;
            }

            return frameConv.ToArray();
        }

    }
}
