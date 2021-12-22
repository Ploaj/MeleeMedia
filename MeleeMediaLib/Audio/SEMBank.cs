using System;

namespace MeleeMedia.Audio
{
    public class SEMBank
    {
        /// <summary>
        /// 
        /// </summary>
        public SEMBankScript[] Scripts { get; set; } = new SEMBankScript[0];

        public override string ToString()
        {
            return $" Count {Scripts.Length}";
        }
    }
}
