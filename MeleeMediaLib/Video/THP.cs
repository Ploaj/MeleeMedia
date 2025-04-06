using System.Collections.Generic;
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
        /// <param name="jpeg"></param>
        public static THP FromJPEG(byte[] Data)
        {
            return new THP(JPEGCONV(Data, true));
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

                int length;

                if (pay == 0xDA)
                {
                    frameConv.Add(code);
                    frameConv.Add(pay);

                    // process until 0xFFD9 is found?
                    int j;
                    int lastff = -1;
                    for (j = i; j < data.Length; j++)
                    {
                        frameConv.Add(data[j]);

                        if (data[j] == 0xFF)
                        {
                            if (encode)
                            {
                                if (j + 1 < data.Length && data[j + 1] != 0xD9)
                                    j++;
                            }
                            else
                            {
                                if (j + 1 < data.Length && 
                                    data[j + 1] == 0xD9)
                                {
                                    lastff = frameConv.Count;
                                }

                                frameConv.Add(0x00);
                            }
                        }
                    }
                    if (lastff != -1)
                        frameConv.RemoveAt(lastff);

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
