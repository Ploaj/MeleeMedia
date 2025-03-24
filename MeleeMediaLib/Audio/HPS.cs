using MeleeMedia.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace MeleeMedia.Audio
{
    public class HPS
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DSP ToDSP(byte[] data)
        {
            DSP dsp = new DSP();
            dsp.Channels.Clear();
            using (BinaryReaderExt r = new BinaryReaderExt(new MemoryStream(data)))
            {
                r.BigEndian = true;

                if (new string(r.ReadChars(7)) != " HALPST")
                    throw new NotSupportedException("Invalid HPS file");
                r.ReadByte();

                dsp.Frequency = r.ReadInt32();

                var channelCount = r.ReadInt32();

                if (channelCount != 2)
                    throw new NotSupportedException("Only HPS with 2 channels are currently supported");

                for (int i = 0; i < channelCount; i++)
                {
                    var channel = new DSPChannel()
                    {
                        LoopFlag = r.ReadInt16(),
                        Format = r.ReadInt16(),
                    };
                    var SA = r.ReadInt32();
                    var EA = r.ReadInt32();
                    var CA = r.ReadInt32();
                    for (int k = 0; k < 0x10; k++)
                        channel.COEF[k] = r.ReadInt16();
                    channel.Gain = r.ReadInt16();
                    channel.InitialPredictorScale = r.ReadInt16();
                    channel.InitialSampleHistory1 = r.ReadInt16();
                    channel.InitialSampleHistory2 = r.ReadInt16();

                    channel.NibbleCount = EA - CA;
                    channel.LoopStart = SA - CA;

                    dsp.Channels.Add(channel);
                }

                // read blocks
                r.Position = 0x80;

                Dictionary<int, int> OffsetToLoopPosition = new Dictionary<int, int>();
                List<byte> channelData1 = new List<byte>();
                List<byte> channelData2 = new List<byte>();
                while (true)
                {
                    var pos = r.Position;
                    var length = r.ReadInt32();
                    var lengthMinusOne = r.ReadInt32();
                    var next = r.ReadInt32();
                    {
                        var initPS = r.ReadInt16();
                        var initsh1 = r.ReadInt16();
                        var initsh2 = r.ReadInt16();
                        var gain = r.ReadInt16();
                    }
                    {
                        var initPS = r.ReadInt16();
                        var initsh1 = r.ReadInt16();
                        var initsh2 = r.ReadInt16();
                        var gain = r.ReadInt16();
                    }
                    var extra = r.ReadInt32();

                    OffsetToLoopPosition.Add((int)pos, channelData1.Count * 2);
                    channelData1.AddRange(r.ReadBytes(length / 2));
                    channelData2.AddRange(r.ReadBytes(length / 2));

                    if (next < r.Position || next == -1)
                    {
                        if (next != -1)
                        {
                            foreach (var c in dsp.Channels)
                            {
                                c.LoopStart = OffsetToLoopPosition[next];
                            }
                        }
                        else
                        {
                            dsp.LoopSound = false;
                        }
                        break;
                    }
                    else
                        r.Position = (uint)next;
                }

                dsp.Channels[0].Data = channelData1.ToArray();
                dsp.Channels[1].Data = channelData2.ToArray();
            }

            return dsp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <param name="filePath"></param>
        public static void WriteDSPAsHPS(DSP dsp, Stream s)
        {
            using (BinaryWriterExt w = new BinaryWriterExt(s))
            {
                w.BigEndian = true;

                w.Write(" HALPST".ToCharArray());
                w.Write((byte)0);
                w.Write(dsp.Frequency);
                w.Write(dsp.Channels.Count);

                // channel meta data
                foreach (var c in dsp.Channels)
                {
                    w.Write((short)1);
                    w.Write(c.Format);
                    w.Write(2);
                    w.Write(dsp.Channels[0].Data.Length * 2);
                    w.Write(2);
                    for (int k = 0; k < 0x10; k++)
                        w.Write(c.COEF[k]);
                    w.Write(c.Gain);
                    w.Write(c.InitialPredictorScale);
                    w.Write(c.InitialSampleHistory1);
                    w.Write(c.InitialSampleHistory2);
                }


                // now divide into chunks taking into account loop point

                var loopStart = dsp.Channels[0].LoopStart / 2;
                if (dsp.LoopSound)
                {
                    if (loopStart != 0 &&
                        loopStart % 56 == 0)
                        loopStart += 56 - (loopStart % 56);
                }
                else
                {
                    loopStart = 0;
                }
                var loopPosition = -1;
                var nextPosition = 0;

                int[] history = new int[dsp.Channels.Count];

                // preprocess - determine loop chunk size
                
                // calculate general chunk size
                var baseChunkSize = 0x10000 / dsp.Channels.Count;

                // calculate size of loop chunk
                var loopChunk = loopStart % baseChunkSize;
                var loopIndex = loopStart / baseChunkSize;

                // check if loop buffer too small
                // if it is then just take data from the chunk before it
                bool loopSplit = false;
                if (loopIndex != 0 && loopChunk * 2 < 0x10000 / 2)
                    loopSplit = true;

                var dataLength = dsp.Channels[0].Data.Length;

                int i;
                int chunkIndex = 0;
                for (i = 0; i < dataLength;)
                {   
                    // calculate general chunk size
                    var chunkSize = baseChunkSize;

                    // check if there is a loop chunk
                    if (loopChunk > 0)
                    {
                        // loop point starts after loop chunk
                        if (chunkIndex == loopIndex + 1)
                            loopPosition = (int)w.BaseStream.Position;

                        // process loop chunk
                        if (chunkIndex == loopIndex)
                        {
                            if (loopSplit)
                            {
                                chunkSize = loopChunk + baseChunkSize / 2;
                            }
                            else
                            {
                                chunkSize = loopChunk;
                            }
                        }

                        // if loop chunk is too small, then take some data from the chunk before it
                        if (chunkIndex == loopIndex - 1)
                        {
                            if (loopSplit)
                            {
                                chunkSize = baseChunkSize - baseChunkSize / 2;
                            }
                        }
                    }
                    else
                    {
                        if (dsp.LoopSound &&
                            chunkIndex == loopIndex)
                            loopPosition = (int)w.BaseStream.Position;
                    }

                    // make sure chunk size is not out of range
                    if (i + chunkSize > dataLength)
                        chunkSize = dataLength - i;

                    // keep track of actual data size
                    int actual_size = chunkSize;

                    // align to 0x20
                    if (chunkSize % 0x20 != 0)
                        chunkSize += 0x20 - (chunkSize % 0x20);

                    // write chunk header
                    var chunkStart = w.BaseStream.Position;
                    w.BaseStream.Position = nextPosition;
                    if (nextPosition != 0)
                        w.Write((int)chunkStart);
                    w.BaseStream.Position = chunkStart;

                    w.Write(chunkSize * 2);
                    w.Write(actual_size * 2 - 1);

                    //System.Diagnostics.Debug.WriteLine(chunkIndex + " " + (chunkSize * 2).ToString("X") + " " + (actual_size * 2).ToString("X"));

                    nextPosition = (int)w.BaseStream.Position;
                    w.Write(0); // next offsets

                    // write history data
                    for (var j = 0; j < history.Length; j++)
                    {
                        w.Write((short)dsp.Channels[j].Data[i]);
                        w.Write(history[j]);
                        w.Write((short)0);
                    }

                    w.Write(0); //padding

                    // write channel data
                    for (var j = 0; j < dsp.Channels.Count; j++)
                    {
                        var c = dsp.Channels[j];

                        // create chunk buffer
                        byte[] chunk = new byte[chunkSize];
                        Array.Copy(c.Data, i, chunk, 0, actual_size);

                        byte[] historyChunk = new byte[4];
                        Array.Copy(c.Data, i, historyChunk, 0, 4);

                        // keep track of decoded history
                        var dec = GcAdpcmDecoder.Decode(historyChunk, c.COEF);
                        history[j] = (ushort)dec[dec.Length - 2] | ((ushort)dec[dec.Length - 1] << 16);

                        // write chunk
                        w.Write(chunk);
                    }

                    // advance data parsing
                    i += chunkSize;
                    chunkIndex++;
                }

                // write loop offset
                w.BaseStream.Position = nextPosition;
                w.Write(loopPosition);
            }
        }

    }
}
