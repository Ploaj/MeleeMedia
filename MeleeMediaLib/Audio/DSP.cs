using MeleeMedia.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                return TimeSpan.FromMilliseconds(LoopPointMilliseconds).ToString();
            }
            set
            {
                if (TimeSpan.TryParse(value, out TimeSpan ts))
                {
                    LoopPointMilliseconds = ts.TotalMilliseconds;
                }
            }
        }

        public double LoopPointMilliseconds
        {
            get
            {
                if (Channels.Count == 0) return 0;
                return Channels[0].LoopStart / Channels.Count / (double)Frequency * 1.75f * 1000;
            }
            set
            {
                var sec = (value / 1.75f / 1000f) * Channels.Count * Frequency;

                foreach (var c in Channels)
                    c.LoopStart = (int)sec;
            }
        }

        public bool LoopSound
        {
            get
            {
                return Channels.Count > 0 && Channels[0].LoopFlag == 1;
            }
            set
            {
                foreach (var c in Channels)
                    c.LoopFlag = (short)(value ? 1 : 0);
            }
        }

        public double TotalMilliseconds
        {
            get
            {
                if (Channels.Count == 0) return 0;
                return Channels[0].Data.Length / (double)Frequency * 1.75f * 1000;
            }
        }

        public string Length
        {
            get
            {
                return TimeSpan.FromMilliseconds(TotalMilliseconds).ToString();
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
        public bool FromFile(string filePath)
        {
            return FromFormat(File.ReadAllBytes(filePath), Path.GetExtension(filePath));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="format"></param>
        public bool FromFormat(byte[] data, string format)
        {
            try
            {
                switch (format.ToLower())
                {
                    case ".brstm":
                        FromBRSTM(data);
                        return true;
                    case ".wav":
                        FromWAVE(data);
                        return true;
                    case ".dsp":
                        FromDSP(data);
                        return true;
                    case ".hps":
                        FromHPS(data);
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
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
                    File.WriteAllBytes(filePath, ToWAVE().ToFile());
                    break;
                case ".dsp":
                    ExportDSP(filePath);
                    break;
                case ".hps":
                    using (var fs = new FileStream(filePath, FileMode.Create))
                        HPS.WriteDSPAsHPS(this, fs);
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

                var channel = new DSPChannel()
                {
                    LoopFlag = r.ReadInt16(),
                    Format = r.ReadInt16(),
                };
                var LoopStartOffset = r.ReadInt32();
                r.ReadInt32(); // LoopEndOffset
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wavFile"></param>
        public void FromWAVE(byte[] wavFile)
        {
            var wav = new WAVE();
            wav.Read(wavFile);

            Frequency = wav.Frequency;

            Channels.Clear();
            foreach (var data in wav.Channels)
            {
                var c = new DSPChannel();

                var ss = data;

                c.COEF = GcAdpcmCoefficients.CalculateCoefficients(ss);

                c.Data = GcAdpcmEncoder.Encode(ss, c.COEF);

                c.NibbleCount = GcAdpcmMath.SampleCountToNibbleCount(ss.Length);

                c.InitialPredictorScale = c.Data[0];

                Channels.Add(c);
            }

            if (wav.LoopPoint != 0)
            {
                LoopSound = true;
                LoopPointMilliseconds = wav.LoopPoint / (double)wav.Frequency * 1000.0;
            }
        }
        /// <summary>
        /// Wraps the decoded DSP data into a WAVE file stored as a byte array
        /// </summary>
        /// <returns></returns>
        public WAVE ToWAVE()
        {
            WAVE wav = new WAVE()
            {
                Frequency = Frequency,
                BitsPerSample = 16,
            };

            foreach (var c in Channels)
            {
                wav.Channels.Add(GcAdpcmDecoder.Decode(c.Data, c.COEF));
            }

            if (LoopSound)
            {
                wav.LoopPoint = (int)(LoopPointMilliseconds / 1000.0 * wav.Frequency);
            }

            return wav;
        }

        #endregion

        #region BRSTM

        /// <summary>
        /// 
        /// </summary>
        public enum BRSTM_CODEC
        {
            PCM_8bit,
            PCM16bit,
            ADPCM_4bit,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <param name="filePath"></param>
        public void FromBRSTM(string filePath)
        {
            using (FileStream s = new FileStream(filePath, FileMode.Open))
                FromBRSTM(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <param name="filePath"></param>
        public void FromBRSTM(byte[] filedata)
        {
            using (MemoryStream s = new MemoryStream(filedata))
                FromBRSTM(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsp"></param>
        /// <param name="filePath"></param>
        public void FromBRSTM(Stream stream)
        {
            Channels.Clear();
            using (BinaryReaderExt r = new BinaryReaderExt(stream))
            {
                if (new string(r.ReadChars(4)) != "RSTM")
                    throw new NotSupportedException("File is not a valid BRSTM file");

                r.BigEndian = true;
                r.BigEndian = r.ReadUInt16() == 0xFEFF;

                r.Skip(2); // 01 00 version
                r.Skip(4); // filesize

                r.Skip(2); // 00 40 - header length 
                r.Skip(2); // 00 02 - header version 

                var headOffset = r.ReadUInt32();
                var headSize = r.ReadUInt32();

                var adpcOffset = r.ReadUInt32();
                var adpcSize = r.ReadUInt32();

                var dataOffset = r.ReadUInt32();
                var dataSize = r.ReadUInt32();


                // can skip adpc section when reading because it just contains sample history


                // parse head section
                // --------------------------------------------------------------
                r.Position = headOffset;
                if (new string(r.ReadChars(4)) != "HEAD")
                    throw new NotSupportedException("BRSTM does not have a valid HEAD");
                r.Skip(4); // section size


                r.Skip(4); // 01 00 00 00 marker
                var chunk1Offset = r.ReadUInt32() + 8 + headOffset;

                r.Skip(4); // 01 00 00 00 marker
                var chunk2Offset = r.ReadUInt32() + 8 + headOffset;

                r.Skip(4); // 01 00 00 00 marker
                var chunk3Offset = r.ReadUInt32() + 8 + headOffset;


                // --------------------------------------------------------------
                r.Seek(chunk1Offset);
                var codec = (BRSTM_CODEC)r.ReadByte();
                var loopFlag = r.ReadByte();
                var channelCount = r.ReadByte();
                r.Skip(1); // padding

                if (codec != BRSTM_CODEC.ADPCM_4bit)
                    throw new NotSupportedException("only 4bit ADPCM files currently supported");

                var sampleRate = r.ReadUInt16();
                r.Skip(2); // padding

                Frequency = sampleRate;

                var loopStart = r.ReadUInt32();
                var totalSamples = r.ReadUInt32();

                var dataPointer = r.ReadUInt32(); // DATA offset
                int blockCount = r.ReadInt32();

                var blockSize = r.ReadUInt32();
                var samplesPerBlock = r.ReadInt32();

                var sizeOfFinalBlock = r.ReadUInt32();
                var samplesInFinalBlock = r.ReadInt32();

                var sizeOfFinalBlockWithPadding = r.ReadUInt32();

                var samplesPerEntry = r.ReadInt32();
                var bytesPerEntry = r.ReadInt32();

                // --------------------------------------------------------------
                r.Seek(chunk2Offset);
                var numOfTracks = r.ReadByte();
                var trackDescType = r.ReadByte();
                r.Skip(2); // padding

                for (uint i = 0; i < numOfTracks; i++)
                {
                    r.Seek(chunk1Offset + 4 + 8 * i);
                    r.Skip(1); // 01 padding
                    var descType = r.ReadByte();
                    r.Skip(2); // padding
                    var descOffset = r.ReadUInt32() + 8 + headOffset;

                    r.Seek(descOffset);
                    switch (descType)
                    {
                        case 0:
                            {
                                int channelsInTrack = r.ReadByte();
                                int leftChannelID = r.ReadByte();
                                int rightChannelID = r.ReadByte();
                                r.Skip(1); // padding
                            }
                            break;
                        case 1:
                            {
                                var volume = r.ReadByte();
                                var panning = r.ReadByte();
                                r.Skip(2); // padding
                                r.Skip(4); // padding
                                int channelsInTrack = r.ReadByte();
                                int leftChannelID = r.ReadByte();
                                int rightChannelID = r.ReadByte();
                                r.Skip(1); // 01 padding
                            }
                            break;
                    }
                }

                // --------------------------------------------------------------
                r.Seek(chunk3Offset);

                var channelCountAgain = r.ReadByte();
                r.Skip(3);

                for (uint i = 0; i < channelCountAgain; i++)
                {
                    r.Seek(chunk3Offset + 4 + 8 * i);

                    r.Skip(4); // 01000000 marker
                    var offset = r.ReadUInt32() + headOffset + 8;

                    r.Seek(offset);

                    // channel information
                    var channel = new DSPChannel();
                    Channels.Add(channel);
                    channel.LoopFlag = loopFlag;
                    channel.LoopStart = (int)loopStart;

                    r.Skip(4); // 01000000 marker
                    r.Skip(4); // offset to coefficients (they follow directly after)

                    for (int k = 0; k < 0x10; k++)
                        channel.COEF[k] = r.ReadInt16();
                    channel.Gain = r.ReadInt16();
                    channel.InitialPredictorScale = r.ReadInt16();
                    channel.InitialSampleHistory1 = r.ReadInt16();
                    channel.InitialSampleHistory2 = r.ReadInt16();
                    channel.LoopPredictorScale = r.ReadInt16();
                    channel.LoopSampleHistory1 = r.ReadInt16();
                    channel.LoopSampleHistory2 = r.ReadInt16();
                    r.Skip(2); // padding

                    // get channel data
                    using (MemoryStream channelStream = new MemoryStream())
                    {
                        for (uint j = 0; j < blockCount; j++)
                        {
                            var bs = blockSize;
                            var actualBlockSize = blockSize;

                            if (j == blockCount - 1)
                            {
                                bs = sizeOfFinalBlockWithPadding;
                                actualBlockSize = sizeOfFinalBlock;
                            }

                            channelStream.Write(r.GetSection(dataPointer + j * (blockSize * channelCountAgain) + bs * i, (int)actualBlockSize), 0, (int)actualBlockSize);
                        }

                        channel.Data = channelStream.ToArray();
                        channel.NibbleCount = channel.Data.Length * 2;
                    }
                }


                LoopPoint = TimeSpan.FromMilliseconds(loopStart / (double)sampleRate * 1000).ToString();
            }
        }
        #endregion

        public override string ToString()
        {
            var loop = LoopSound ? " : " + LoopPoint : "";
            return $"{ChannelType} : {Frequency}Hz : {Length}{loop}";
        }
    }
}
