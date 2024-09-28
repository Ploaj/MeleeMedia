using System;
using System.Collections.Generic;
using System.IO;

namespace MeleeMedia.Audio
{
    public class WAVE
    {
        public int Frequency { get; set; }

        public short BitsPerSample { get; set; } = 16;

        public List<short[]> Channels { get; internal set; } = new List<short[]>();

        public int LoopPoint { get; set; } = 0;

        public IEnumerable<short> RawData
        {
            get
            {
                if (Channels.Count == 1)
                {
                    foreach (var s in Channels[0])
                        yield return s;
                }
                if (Channels.Count == 2)
                {
                    for (int i = 0; i < Math.Min(Channels[0].Length, Channels[1].Length); i++)
                    {
                        yield return Channels[0][i];
                        yield return Channels[1][i];
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public WAVE()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wav"></param>
        public void Append(WAVE wav)
        {
            while (Channels.Count != wav.Channels.Count)
                Channels.Add(new short[0]);

            for (int i = 0; i < wav.Channels.Count; i++)
            {
                var newsize = Channels[i].Length + wav.Channels[i].Length;
                var nc = new short[newsize];
                Array.Copy(Channels[i], 0, nc, 0, Channels[i].Length);
                Array.Copy(wav.Channels[i], 0, nc, Channels[i].Length, wav.Channels[i].Length);
                Channels[i] = nc;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToFile()
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
                        short[] sound_data = Channels[0];
                        subChunkSize += sound_data.Length * 2;
                        foreach (var s in sound_data)
                            w.Write(s);
                    }
                    if (Channels.Count == 2)
                    {
                        short[] sound_data1 = Channels[0];
                        short[] sound_data2 = Channels[1];
                        subChunkSize += (sound_data1.Length + sound_data2.Length) * 2;
                        for (int i = 0; i < sound_data1.Length; i++)
                        {
                            w.Write(sound_data1[i]);
                            w.Write(sound_data2[i]);
                        }
                    }

                    if (LoopPoint != 0)
                    {
                        w.Write("smpl".ToCharArray());
                        w.Write(0x3C); // total size of section

                        w.Write(0); // Manufacturer
                        w.Write(0); // Product
                        w.Write(0); // Sample Period
                        w.Write(0x3C); // MIDI Unity Note
                        w.Write(0); // MIDI Pitch Fraction
                        w.Write(0); // SMPTE Format
                        w.Write(0); // SMPTE Offset
                        w.Write(1); // Loop Samples
                        w.Write(0); // Sampler Data

                        w.Write(0); // Cue Point I
                        w.Write(0); // Type
                        w.Write(LoopPoint); // Start
                        w.Write(Channels[0].Length); // End
                        w.Write(0); // Fraction
                        w.Write(0); // Play Count
                    }

                    w.BaseStream.Position = subchunkOffset;
                    w.Write(subChunkSize);

                    w.BaseStream.Position = 4;
                    w.Write((int)(w.BaseStream.Length - 8));
                }
                return stream.ToArray();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public void Write(string filePath)
        {
            File.WriteAllBytes(filePath, ToFile());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wavFile"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void Read(byte[] wavFile)
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

                while (r.BaseStream.Position < r.BaseStream.Length)
                {
                    var header = new string(r.ReadChars(4));

                    switch (header)
                    {
                        case "data":
                            ReadData(r, channelCount);
                            break;
                        case "smpl":
                            ReadSmpl(r);
                            break;
                        default:
                            var skip = r.ReadInt32();
                            r.BaseStream.Position += skip;
                            break;
                    }
                }

            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="channelCount"></param>
        private void ReadData(BinaryReader r, int channelCount)
        {
            var channelSizes = r.ReadInt32() / channelCount / 2;

            for (int i = 0; i < channelCount; i++)
                Channels.Add(new short[channelSizes]);

            for (int i = 0; i < channelSizes; i++)
                for (int j = 0; j < channelCount; j++)
                {
                    Channels[j][i] = r.ReadInt16();
                }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        private void ReadSmpl(BinaryReader r)
        {
            var size = r.ReadUInt32();
            var end = r.BaseStream.Position + size;
            r.BaseStream.Position += 4; // Manufacturer
            r.BaseStream.Position += 4; // Product
            r.BaseStream.Position += 4; // Sample Period
            r.BaseStream.Position += 4; // MIDI Unity Note
            r.BaseStream.Position += 4; // MIDI Pitch Fraction
            r.BaseStream.Position += 4; // SMPTE Format
            r.BaseStream.Position += 4; // SMPTE Offset
            int loopCount = r.ReadInt32(); // Loop Samples
            r.BaseStream.Position += 4; // Sampler Data

            // TODO: we could better respect loop data by extracting the full sound
            // data with respect to loops, but for the sake of simplicity I'm
            // only going to use the first loop and only the start not the end
            if (loopCount > 1)
                loopCount = 1;

            for (int i = 0; i < loopCount; i++)
            {
                r.BaseStream.Position += 4; // Cue Point I
                r.BaseStream.Position += 4; // Type
                LoopPoint = r.ReadInt32(); // Start TODO: check if this division is needed
                r.BaseStream.Position += 4; // End
                r.BaseStream.Position += 4; // Fraction
                r.BaseStream.Position += 4; // Play Count
            }

            r.BaseStream.Position = end;
        }

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct SmplChunk
        //{
        //    // Chunk Header
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        //    public byte[] chunkID;               // "smpl" in ASCII
        //    public uint chunkSize;               // Size of the chunk data

        //    // Sampler Specific Data
        //    public uint manufacturer;            // Manufacturer ID (MIDI Manufacturer ID)
        //    public uint product;                 // Product ID
        //    public uint samplePeriod;            // In nanoseconds (1/sampleRate)
        //    public uint midiUnityNote;           // MIDI Unity Note (Middle C = 60)
        //    public uint midiPitchFraction;       // Fraction of a semitone (0 = no fine tuning)
        //    public uint smpteFormat;             // SMPTE format (usually 0)
        //    public uint smpteOffset;             // SMPTE offset (usually 0)
        //    public uint numSampleLoops;          // Number of sample loops
        //    public uint samplerData;             // Additional sampler data (usually 0)

        //    // Loops (defined later)
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0)]
        //    public SampleLoop[] sampleLoops;     // Array of sample loops

        //    // Constructor for SMPL Chunk
        //    public SmplChunk(uint numLoops)
        //    {
        //        chunkID = new byte[] { (byte)'s', (byte)'m', (byte)'p', (byte)'l' };
        //        chunkSize = 0;
        //        manufacturer = 0;
        //        product = 0;
        //        samplePeriod = 0;
        //        midiUnityNote = 60; // Default to Middle C
        //        midiPitchFraction = 0;
        //        smpteFormat = 0;
        //        smpteOffset = 0;
        //        numSampleLoops = numLoops;
        //        samplerData = 0;

        //        sampleLoops = new SampleLoop[numLoops];
        //    }
        //}

        //// Loop structure
        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct SampleLoop
        //{
        //    public uint cuePointID;              // Cue point identifier
        //    public uint type;                    // Loop type: 0 = forward, 1 = alternating, 2 = backward
        //    public uint start;                   // Start sample frame of the loop
        //    public uint end;                     // End sample frame of the loop
        //    public uint fraction;                // Fraction of a sample (for finer precision)
        //    public uint playCount;               // Number of times to play the loop (0 = infinite)

        //    // Constructor for Sample Loop
        //    public SampleLoop(uint cuePointID, uint type, uint start, uint end, uint fraction, uint playCount)
        //    {
        //        this.cuePointID = cuePointID;
        //        this.type = type;
        //        this.start = start;
        //        this.end = end;
        //        this.fraction = fraction;
        //        this.playCount = playCount;
        //    }
        //}
    }
}
