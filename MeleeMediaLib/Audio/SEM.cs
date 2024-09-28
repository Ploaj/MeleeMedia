using MeleeMedia.IO;
using System.Collections.Generic;
using System.IO;

namespace MeleeMedia.Audio
{
    public class SEM
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<SEMBank> ReadSEMFile(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
                return ReadSEMFile(stream);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<SEMBank> ReadSEMFile(byte[] data)
        {
            using (var stream = new MemoryStream(data))
                return ReadSEMFile(stream);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<SEMBank> ReadSEMFile(Stream stream)
        {
            var entries = new List<SEMBank>();

            using (BinaryReaderExt r = new BinaryReaderExt(stream))
            {
                r.BigEndian = true;

                r.Seek(8);
                var entryCount = r.ReadInt32();

                var offsetTableStart = r.Position + (entryCount + 1) * 4;

                for (uint i = 0; i < entryCount; i++)
                {
                    SEMBank e = new SEMBank();
                    entries.Add(e);

                    r.Seek(0x0C + i * 4);
                    var startIndex = r.ReadInt32();
                    var endIndex = r.ReadInt32();

                    e.Scripts = new SEMBankScript[endIndex - startIndex];

                    for (uint j = 0; j < endIndex - startIndex; j++)
                    {
                        SEMBankScript s = new SEMBankScript();

                        r.Seek((uint)(offsetTableStart + startIndex * 4 + j * 4));
                        var dataOffsetStart = r.ReadUInt32();
                        var dataOffsetEnd = r.ReadUInt32();

                        if (dataOffsetEnd == 0)
                            dataOffsetEnd = (uint)r.Length;

                        r.Seek(dataOffsetStart);
                        s.Decompile(r.ReadBytes((int)(dataOffsetEnd - dataOffsetStart)));

                        e.Scripts[j] = s;
                    }
                }
            }
            return entries;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="entries"></param>
        public static void SaveSEMFile(string path, List<SEMBank> entries)
        {
            using (var stream = new FileStream(path, FileMode.Create))
                SaveSEMFile(stream, entries);
        }
        /// <summary>
        /// Generates and saves a SEM file
        /// </summary>
        public static void SaveSEMFile(Stream stream, List<SEMBank> entries)
        {
            using (BinaryWriterExt w = new BinaryWriterExt(stream))
            {
                w.BigEndian = true;

                w.Write(0);
                w.Write(0);
                w.Write(entries.Count);
                int index = 0;
                foreach (var e in entries)
                {
                    w.Write(index);
                    index += e.Scripts.Length;
                }
                w.Write(index);

                var offset = w.BaseStream.Position + 4 * index + 4;
                var dataindex = 0;

                foreach (var e in entries)
                {
                    foreach (var v in e.Scripts)
                    {
                        w.Write((int)(offset + dataindex));
                        dataindex += v.Compile().Length;
                    }
                }

                w.Write(0);

                foreach (var e in entries)
                    foreach (var v in e.Scripts)
                        w.Write(v.Compile());
            }
        }
    }
}
