﻿using MeleeMedia.IO;
using System;
using System.IO;

namespace MeleeMedia.Audio
{
    public class SSM
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int StartIndex { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        public DSP[] Sounds { get; set; } = new DSP[0];

        /// <summary>
        /// 
        /// </summary>
        public SSM()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void AddSound(DSP dsp)
        {
            var ar = Sounds;
            Array.Resize(ref ar, ar.Length + 1);
            ar[ar.Length - 1] = dsp;
            Sounds = ar;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RemoveSound(int index)
        {
            if (index < 0 || index >= Sounds.Length)
                return false;

            Sounds = Sounds.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RemoveSound(DSP dsp)
        {
            return RemoveSound(Sounds.IndexOf(dsp));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Open(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
                Open(Path.GetFileName(filePath), fs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Open(string name, Stream s)
        {
            Name = name;
            using (BinaryReaderExt r = new BinaryReaderExt(s))
            {
                r.BigEndian = true;

                var headerLength = r.ReadInt32() + 0x10;
                var dataOff = r.ReadInt32();
                var soundCount = r.ReadInt32();
                StartIndex = r.ReadInt32();

                Sounds = new DSP[soundCount];

                for (int i = 0; i < soundCount; i++)
                {
                    var sound = new DSP();
                    var ChannelCount = r.ReadInt32();
                    sound.Frequency = r.ReadInt32();

                    sound.Channels.Clear();
                    for (int j = 0; j < ChannelCount; j++)
                    {
                        var channel = new DSPChannel()
                        {
                            LoopFlag = r.ReadInt16(),
                            Format = r.ReadInt16(),
                        };
                        var LoopStartOffset = r.ReadInt32();
                        var LoopEndOffset = r.ReadInt32();
                        var CurrentAddress = r.ReadInt32();
                        for (int k = 0; k < 0x10; k++)
                            channel.COEF[k] = r.ReadInt16();
                        channel.Gain = r.ReadInt16();
                        channel.InitialPredictorScale = r.ReadInt16();
                        channel.InitialSampleHistory1 = r.ReadInt16();
                        channel.InitialSampleHistory2 = r.ReadInt16();
                        channel.LoopPredictorScale = r.ReadInt16();
                        channel.LoopSampleHistory1 = r.ReadInt16();
                        channel.LoopSampleHistory2 = r.ReadInt16();
                        r.ReadInt16(); //  padding

                        channel.NibbleCount = LoopEndOffset - CurrentAddress;
                        channel.LoopStart = LoopStartOffset - CurrentAddress;

                        sound.Channels.Add(channel);

                        var DataOffset = headerLength + (int)Math.Ceiling(CurrentAddress / 2d) - 1;

                        channel.Data = r.GetSection((uint)DataOffset, (int)Math.Ceiling(channel.NibbleCount / 2d) + 1);

                    }

                    Sounds[i] = sound;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Save(string filePath)
        {
            Save(filePath, out int _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Save(string filePath, out int bufferSize)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
                WriteToStream(stream, out bufferSize);
        }

        /// <summary>
        /// Writes SSM file to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bufferSize">Size of the buffer</param>
        public void WriteToStream(Stream stream, out int bufferSize)
        {
            using (BinaryWriterExt w = new BinaryWriterExt(stream))
            {
                w.BigEndian = true;

                w.Write(0);
                w.Write(0);
                w.Write(Sounds.Length);
                w.Write(StartIndex);

                int headerSize = 0;
                foreach (var s in Sounds)
                {
                    headerSize += 8 + s.Channels.Count * 0x40;
                }

                var projData = headerSize + 0x20;
                foreach (var s in Sounds)
                {
                    w.Write(s.Channels.Count);
                    w.Write(s.Frequency);

                    foreach (var channel in s.Channels)
                    {
                        var sa = (projData - (headerSize + 0x20) + 1) * 2;

                        projData += channel.Data.Length;
                        if (projData % 0x8 != 0)
                            projData += 0x08 - projData % 0x08;

                        var en = sa + channel.NibbleCount;

                        w.Write(channel.LoopFlag);
                        w.Write(channel.Format);
                        w.Write(sa + channel.LoopStart);
                        w.Write(en);
                        w.Write(sa);
                        foreach (var v in channel.COEF)
                            w.Write(v);
                        w.Write(channel.Gain);
                        w.Write(channel.InitialPredictorScale);
                        w.Write(channel.InitialSampleHistory1);
                        w.Write(channel.InitialSampleHistory2);
                        w.Write(channel.LoopPredictorScale);
                        w.Write(channel.LoopSampleHistory1);
                        w.Write(channel.LoopSampleHistory2);
                        w.Write((short)0);
                    }

                }

                var start = w.BaseStream.Position;
                foreach (var s in Sounds)
                {
                    foreach (var c in s.Channels)
                    {
                        w.Write(c.Data);
                        if (w.BaseStream.Position % 0x08 != 0)
                            w.Write(new byte[0x08 - w.BaseStream.Position % 0x08]);
                    }

                }

                // align 0x20
                if (w.BaseStream.Position % 0x20 != 0)
                    w.Write(new byte[0x20 - w.BaseStream.Position % 0x20]);

                var DataSize = w.BaseStream.Position - start;

                if (DataSize % 0x20 != 0)
                {
                    w.Write(new byte[0x20 - DataSize % 0x20]);
                    w.Write(0);
                    w.Write(0);
                    DataSize += 0x20 - DataSize % 0x20;
                }

                w.Seek(0);
                w.Write(headerSize);
                w.Write((int)DataSize);

                bufferSize = (int)DataSize;
            }
        }
    }
}
