﻿using MeleeMedia.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeleeMedia.Audio
{
    /// <summary>
    /// Custom class for handling sound banks + scripts together
    /// </summary>
    public class SEMPackage
    {
        /// <summary>
        /// 
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public uint GroupFlags { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SEMBank ScriptBank { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public SSM SoundBank { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptBank"></param>
        /// <param name="soundBank"></param>
        public SEMPackage(SEMBank scriptBank, SSM soundBank)
        {
            ScriptBank = scriptBank;
            SoundBank = soundBank;
        }

        /// <summary>
        /// Removes unused sounds from sound bank
        /// </summary>
        public void RemoveUnusedSoundsFromBank()
        {
            if (ScriptBank == null || SoundBank == null)
                return;

            var usedSounds = ScriptBank.Scripts.Select(e => e.SFXID);

            List<DSP> newList = new List<DSP>();

            for (int i = 0; i < SoundBank.Sounds.Length; i++)
            {
                if (usedSounds.Contains(i))
                    newList.Add(SoundBank.Sounds[i]);
            }

            SoundBank.Sounds = newList.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadFromPackage(string fileName)
        {
            using (FileStream s = new FileStream(fileName, FileMode.Open))
            using (BinaryReaderExt r = new BinaryReaderExt(s))
            {
                if (s.Length < 0x14)
                    return;

                if (new string(r.ReadChars(4)) != "SPKG")
                    return;

                GroupFlags = r.ReadUInt32();
                Flags = r.ReadUInt32();

                var ssmSize = r.ReadInt32();
                ScriptBank = new SEMBank()
                {
                    Scripts = new SEMBankScript[r.ReadInt32()]
                };

                for (int i = 0; i < ScriptBank.Scripts.Length; i++)
                {
                    ScriptBank.Scripts[i] = new SEMBankScript();
                    ScriptBank.Scripts[i].Decompile(r.GetSection(r.ReadUInt32(), r.ReadInt32()));
                }

                var name = r.ReadString(r.ReadByte());

                if (ssmSize == 0)
                {
                    SoundBank = null;
                }
                else
                {
                    SoundBank = new SSM();
                    using (MemoryStream ssmStream = new MemoryStream(r.ReadBytes(ssmSize)))
                        SoundBank.Open(name, ssmStream);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveAsPackage(string fileName)
        {
            using (FileStream s = new FileStream(fileName, FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(s))
            {
                w.Write(new char[] { 'S', 'P', 'K', 'G' });

                w.Write(GroupFlags);
                w.Write(Flags);
                w.Write(0);
                w.Write(ScriptBank.Scripts.Length);

                w.Write(new byte[ScriptBank.Scripts.Length * 8]);

                w.Write(SoundBank != null ? SoundBank.Name : "");

                if (SoundBank != null)
                    using (MemoryStream ssmFile = new MemoryStream())
                    {
                        SoundBank.WriteToStream(ssmFile, out int bs);
                        var ssm = ssmFile.ToArray();
                        w.Write(ssm);
                        var temp = s.Position;
                        s.Position = 0x0C;
                        w.Write(ssm.Length);
                        s.Position = temp;
                    }

                for (int i = 0; i < ScriptBank.Scripts.Length; i++)
                {
                    var commandData = ScriptBank.Scripts[i].Compile();

                    var temp = s.Position;
                    s.Position = 0x14 + 8 * i;

                    w.Write((int)temp);
                    w.Write(commandData.Length);

                    s.Position = temp;

                    w.Write(commandData);
                }
            }
        }
    }
}