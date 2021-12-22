﻿using MeleeMedia.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace MeleeMedia.Audio
{
    public class DSPChannel
    {
        public short LoopFlag { get; set; }
        public short Format { get; set; }
        public short[] COEF = new short[0x10];
        public short Gain { get; set; }
        public short InitialPredictorScale { get; set; }
        public short InitialSampleHistory1 { get; set; }
        public short InitialSampleHistory2 { get; set; }
        public short LoopPredictorScale { get; set; }
        public short LoopSampleHistory1 { get; set; }
        public short LoopSampleHistory2 { get; set; }

        public byte[] Data;

        public int LoopStart { get; set; } = 0;
        public int NibbleCount = 0;

        public override string ToString()
        {
            return "Channel";
        }
    }

    public class DSP
    {

        // TODO: *.mp3*.aiff*.wma*.m4a
        public static string SupportedImportFilter { get; } = "Supported (*.dsp*.wav*.hps)|*.dsp;*.wav;*.hps;";

        public static string SupportedExportFilter { get; } = "Supported Types(*.wav*.dsp*.hps)|*.wav;*.dsp;*.hps";

        public int Frequency { get; set; }

        public string LoopPoint
        {
            get
            {
                if (Channels.Count == 0)
                    return "0:00";
                var sec = (int)Math.Ceiling(Channels[0].LoopStart / 2 / (double)Frequency * 1.75f * 1000);
                return TimeSpan.FromMilliseconds(sec).ToString();
            }
            set
            {
                TimeSpan ts;
                if (TimeSpan.TryParse(value, out ts))
                {
                    SetLoopFromTimeSpan(ts);
                }
            }
        }

        public string Length
        {
            get
            {
                if (Channels.Count == 0)
                    return "0:00";
                var sec = (int)Math.Ceiling(Channels[0].Data.Length / (double)Frequency * 1.75f * 1000);
                return TimeSpan.FromMilliseconds(sec).ToString();
            }
        }

        public string ChannelType
        {
            get
            {
                if (Channels == null)
                    return "";
                if (Channels.Count == 1)
                    return "Mono";
                if (Channels.Count == 2)
                    return "Stereo";
                return "";
            }
        }

        public List<DSPChannel> Channels = new List<DSPChannel>();

        public void SetLoopFromTimeSpan(TimeSpan s)
        {
            var sec = (s.TotalMilliseconds / 1.75f / 1000f) * 2 * Frequency;

            foreach (var c in Channels)
                c.LoopStart = (int)sec;
        }

        /// <summary>
        /// 
        /// </summary>

        public DSP()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public DSP(string filePath)
        {
            FromFile(filePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void FromFile(string filePath)
        {
            FromFormat(File.ReadAllBytes(filePath), Path.GetExtension(filePath));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="format"></param>
        public void FromFormat(byte[] data, string format)
        {
            format = format.Replace(".", "").ToLower();

            switch (format)
            {
                case "wav":
                    FromWAVE(data);
                    break;
                case "dsp":
                    FromDSP(data);
                    break;
                case "hps":
                    FromHPS(data);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void ExportFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();

            switch (ext)
            {
                case ".wav":
                    File.WriteAllBytes(filePath, ToWAVE());
                    break;
                case ".dsp":
                    ExportDSP(filePath);
                    break;
                case ".hps":
                    HPS.SaveDSPAsHPS(this, filePath);
                    break;
            }
        }

        #region DSP

        private void FromDSP(byte[] data)
        {
            Channels.Clear();
            using (BinaryReaderExt r = new BinaryReaderExt(new MemoryStream(data)))
            {
                r.BigEndian = true;

                r.ReadInt32();
                var nibbleCount = r.ReadInt32();
                Frequency = r.ReadInt32();

                var channel = new DSPChannel();

                channel.LoopFlag = r.ReadInt16();
                channel.Format = r.ReadInt16();
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

                r.Seek(0x60);
                channel.NibbleCount = nibbleCount;
                channel.LoopStart = LoopStartOffset - CurrentAddress;
                channel.Data = r.ReadBytes((int)Math.Ceiling(nibbleCount / 2d));

                Channels.Add(channel);

                r.BaseStream.Close();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        private void ExportDSP(string filePath)
        {
            if (Channels.Count == 1)
            {
                // mono
                ExportDSPChannel(filePath, Channels[0]);
            }
            else
            {
                // stereo or more
                var head = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);

                for (int i = 0; i < Channels.Count; i++)
                {

                    ExportDSPChannel(head + $"_channel_{i}" + ext, Channels[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="channel"></param>
        private void ExportDSPChannel(string filePath, DSPChannel channel)
        {
            using (BinaryWriterExt w = new BinaryWriterExt(new FileStream(filePath, FileMode.Create)))
            {
                w.BigEndian = true;

                var samples = channel.NibbleCount * 7 / 8;

                w.Write(samples);
                w.Write(channel.NibbleCount);
                w.Write(Frequency);

                w.Write(channel.LoopFlag);
                w.Write(channel.Format);
                w.Write(2);
                w.Write(channel.NibbleCount - 2);
                w.Write(2);
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

                w.Write(new byte[0x14]);

                w.Write(channel.Data);

                if (w.BaseStream.Position % 0x8 != 0)
                    w.Write(new byte[0x08 - w.BaseStream.Position % 0x08]);

                w.BaseStream.Close();
            }
        }

        #endregion

        #region HPS

        private void FromHPS(byte[] data)
        {
            var dsp = HPS.ToDSP(data);
            Channels = dsp.Channels;
            Frequency = dsp.Frequency;
        }

        /*public void ToHPS()
        {

        }*/

        #endregion

        #region WAVE

        public void FromWAVE(byte[] wavFile)
        {
            if (wavFile.Length < 0x2C)
                throw new NotSupportedException("File is not a valid WAVE file");

            Channels.Clear();

            using (BinaryReader r = new BinaryReader(new MemoryStream(wavFile)))
            {
                if (new string(r.ReadChars(4)) != "RIFF")
                    throw new NotSupportedException("File is not a valid WAVE file");

                r.BaseStream.Position = 0x14;
                var comp = r.ReadInt16();
                var channelCount = r.ReadInt16();
                Frequency = r.ReadInt32();
                r.ReadInt32();// block rate
                r.ReadInt16();// block align
                var bpp = r.ReadInt16();

                if (comp != 1)
                    throw new NotSupportedException("Compressed WAVE files not supported");

                if (bpp != 16)
                    throw new NotSupportedException("Only 16 bit WAVE formats accepted");

                while (r.ReadByte() == 0) ;
                r.BaseStream.Seek(-1, SeekOrigin.Current);

                while (new string(r.ReadChars(4)) != "data")
                {
                    var skip = r.ReadInt32();
                    r.BaseStream.Position += skip;
                }

                var channelSizes = r.ReadInt32() / channelCount / 2;

                List<List<short>> channels = new List<List<short>>();

                for (int i = 0; i < channelCount; i++)
                    channels.Add(new List<short>());

                for (int i = 0; i < channelSizes; i++)
                {
                    foreach (var v in channels)
                    {
                        v.Add(r.ReadInt16());
                    }
                }

                Channels.Clear();
                foreach (var data in channels)
                {
                    var c = new DSPChannel();

                    var ss = data.ToArray();

                    c.COEF = GcAdpcmCoefficients.CalculateCoefficients(ss);

                    c.Data = GcAdpcmEncoder.Encode(ss, c.COEF);

                    c.NibbleCount = (c.Data.Length - 1) * 2;

                    c.InitialPredictorScale = c.Data[0];

                    Channels.Add(c);
                }

            }
        }

        /// <summary>
        /// Wraps the decoded DSP data into a WAVE file stored as a byte array
        /// </summary>
        /// <returns></returns>
        public byte[] ToWAVE()
        {
            using (var stream = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(stream))
                {
                    w.Write("RIFF".ToCharArray());
                    w.Write(0); // wave size

                    w.Write("WAVE".ToCharArray());

                    short BitsPerSample = 16;
                    var byteRate = Frequency * Channels.Count * BitsPerSample / 8;
                    short blockAlign = (short)(Channels.Count * BitsPerSample / 8);

                    w.Write("fmt ".ToCharArray());
                    w.Write(16); // chunk size
                    w.Write((short)1); // compression
                    w.Write((short)Channels.Count);
                    w.Write(Frequency);
                    w.Write(byteRate);
                    w.Write(blockAlign);
                    w.Write(BitsPerSample);

                    w.Write("data".ToCharArray());
                    var subchunkOffset = w.BaseStream.Position;
                    w.Write(0);

                    int subChunkSize = 0;
                    if (Channels.Count == 1)
                    {
                        short[] sound_data = GcAdpcmDecoder.Decode(Channels[0].Data, Channels[0].COEF);
                        subChunkSize += sound_data.Length * 2;
                        foreach (var s in sound_data)
                            w.Write(s);
                    }
                    if (Channels.Count == 2)
                    {
                        short[] sound_data1 = GcAdpcmDecoder.Decode(Channels[0].Data, Channels[0].COEF);
                        short[] sound_data2 = GcAdpcmDecoder.Decode(Channels[1].Data, Channels[1].COEF);
                        subChunkSize += (sound_data1.Length + sound_data2.Length) * 2;
                        for (int i = 0; i < sound_data1.Length; i++)
                        {
                            w.Write(sound_data1[i]);
                            w.Write(sound_data2[i]);
                        }
                    }

                    w.BaseStream.Position = subchunkOffset;
                    w.Write(subChunkSize);

                    w.BaseStream.Position = 4;
                    w.Write((int)(w.BaseStream.Length - 8));
                }
                return stream.ToArray();
            }
        }

        #endregion

        public override string ToString()
        {
            var loop = LoopPoint == "00:00:00" ? "" : " : " + LoopPoint;
            return $"{ChannelType} : {Frequency}Hz : {Length}{loop}";
        }
    }
}
