using System.Collections.Generic;
using System.ComponentModel;

namespace MeleeMedia.Audio
{
    /// <summary>
    /// 
    /// </summary>
    public class SEMBankScript
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<SEMCode> Codes = new List<SEMCode>();

        /// <summary>
        /// Returns the scripts sfxid if it has one and -1 otherwise
        /// </summary>
        [Browsable(false)]
        public int SFXID
        {
            get
            {
                var sfx = Codes.FindLast(e => e.Code == SEM_CODE.SET_SFXID);

                if (sfx != null)
                    return sfx.Value;

                return -1;
            }
            set
            {
                var sfx = Codes.FindLast(e => e.Code == SEM_CODE.SET_SFXID);

                if (sfx != null)
                    sfx.Value = value;
            }
        }

        /// <summary>
        /// Removes unused codes from the script
        /// </summary>
        private void Clean()
        {
            Codes.RemoveAll(e => e.Code == SEM_CODE.NULL);
        }

        /// <summary>
        /// Loads script from op codes
        /// </summary>
        public void Decompile(byte[] code)
        {
            for(int i = 0; i < code.Length; i += 4)
            {
                var script = ((code[i] & 0xFF) << 24) | ((code[i + 1] & 0xFF) << 16) | ((code[i + 2] & 0xFF) << 8) | (code[i + 3] & 0xFF);
                Codes.Add(new SEMCode((uint)script));
            }
        }

        /// <summary>
        /// exports script to op codes
        /// </summary>
        /// <returns></returns>
        public byte[] Compile()
        {
            byte[] codes = new byte[Codes.Count * 4];
            int i = 0;

            foreach (var code in Codes)
            {
                var script = code.Pack();
                codes[i++] = (byte)((script >> 24) & 0xFF);
                codes[i++] = (byte)((script >> 16) & 0xFF);
                codes[i++] = (byte)((script >> 8) & 0xFF);
                codes[i++] = (byte)(script & 0xFF);
            }

            return codes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Name} - ID: {SFXID}";
        }
    }
}
