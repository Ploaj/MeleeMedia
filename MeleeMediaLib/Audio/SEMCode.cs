using System;

namespace MeleeMedia.Audio
{
    /// <summary>
    /// 
    /// </summary>
    public enum SEM_CODE
    {
        SET_TIMER,
        SET_SFXID,
        SET_LOOP,
        EXECUTE_LOOP,
        SET_PRIORITY,
        ADD_PRIORITY,
        PLAY,
        PLAY_ADD_VOLUME,
        SET_CHANNEL_BALANCE,
        ADD_CHANNEL_BALANCE,
        SET_UNUSED,
        ADD_UNUSED,
        SET_PITCH,
        ADD_PITCH,
        END_PLAYBACK,
        LOOP_PLAYBACK,
        SET_REVERB1,
        ADD_REVERB1,
        SET_REVERB2,
        ADD_REVERB2,
        SET_REVERB3,
        SET_REVERB4,
        NULL = 0xFD
    }

    /// <summary>
    /// 
    /// </summary>
    public class SEMCode
    {
        public SEM_CODE Code { get; set; }

        public int Value
        {
            get => _value; 
            set
            {
                _value = Math.Max(MinValue, Math.Min(value, MaxValue));
            }
        }

        private int _value;

        public int Timer { get => _timer; set => _timer = Math.Max(0, Math.Min(value, MaxTimer)); }

        private int _timer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        public SEMCode(SEM_CODE code)
        {
            Code = code;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        public SEMCode(uint script)
        {
            Unpack(script);
        }

        public void Unpack(uint script)
        {
            Code = (SEM_CODE)((script >> 24) & 0xFF);
            switch (Code)
            {
                case SEM_CODE.SET_TIMER:
                    Timer = (int)(script & 0xFFFFFF);
                    break;
                case SEM_CODE.SET_SFXID:
                case SEM_CODE.NULL:
                case SEM_CODE.SET_LOOP:
                case SEM_CODE.EXECUTE_LOOP:
                case SEM_CODE.SET_PRIORITY:
                case SEM_CODE.ADD_PRIORITY:
                case SEM_CODE.END_PLAYBACK:
                case SEM_CODE.LOOP_PLAYBACK:
                case SEM_CODE.SET_REVERB3:
                case SEM_CODE.SET_REVERB4:
                    Value = (int)(script & 0xFFFFFF);
                    break;
                case SEM_CODE.PLAY_ADD_VOLUME:
                case SEM_CODE.SET_CHANNEL_BALANCE:
                case SEM_CODE.ADD_CHANNEL_BALANCE:
                case SEM_CODE.SET_UNUSED:
                case SEM_CODE.ADD_UNUSED:
                case SEM_CODE.SET_REVERB1:
                case SEM_CODE.ADD_REVERB1:
                case SEM_CODE.SET_REVERB2:
                case SEM_CODE.ADD_REVERB2:
                    Timer = (ushort)((script >> 8) & 0xFFFF);
                    Value = (sbyte)(script & 0xFF);
                    break;
                case SEM_CODE.PLAY:
                    Timer = (ushort)((script >> 8) & 0xFFFF);
                    Value = (byte)(script & 0xFF);
                    break;
                case SEM_CODE.SET_PITCH:
                case SEM_CODE.ADD_PITCH:
                    Timer = (byte)((script >> 16) & 0xFF);
                    Value = (short)(script & 0xFFFF);
                    break;
            }
        }

        public uint Pack()
        {
            int script = ((byte)Code & 0xFF) << 24;
            switch (Code)
            {
                case SEM_CODE.SET_TIMER:
                    script |= Timer & 0xFFFFFF;
                    break;
                case SEM_CODE.SET_SFXID:
                case SEM_CODE.NULL:
                case SEM_CODE.SET_LOOP:
                case SEM_CODE.EXECUTE_LOOP:
                case SEM_CODE.SET_PRIORITY:
                case SEM_CODE.ADD_PRIORITY:
                case SEM_CODE.END_PLAYBACK:
                case SEM_CODE.LOOP_PLAYBACK:
                case SEM_CODE.SET_REVERB3:
                case SEM_CODE.SET_REVERB4:
                    script |= Value & 0xFFFFFF;
                    break;
                case SEM_CODE.PLAY:
                case SEM_CODE.PLAY_ADD_VOLUME:
                case SEM_CODE.SET_CHANNEL_BALANCE:
                case SEM_CODE.ADD_CHANNEL_BALANCE:
                case SEM_CODE.SET_UNUSED:
                case SEM_CODE.ADD_UNUSED:
                case SEM_CODE.SET_REVERB1:
                case SEM_CODE.ADD_REVERB1:
                case SEM_CODE.SET_REVERB2:
                case SEM_CODE.ADD_REVERB2:
                    script |= (Timer & 0xFFFF) << 8;
                    script |= Value & 0xFF;
                    break;
                case SEM_CODE.SET_PITCH:
                case SEM_CODE.ADD_PITCH:
                    script |= (Timer & 0xFF) << 16;
                    script |= Value & 0xFFFF;
                    break;
            }
            return (uint)script;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetValueTypeName(SEM_CODE code)
        {
            switch (code)
            {
                case SEM_CODE.SET_TIMER:
                case SEM_CODE.END_PLAYBACK:
                case SEM_CODE.LOOP_PLAYBACK:
                default:
                    return null;
                case SEM_CODE.SET_SFXID:
                case SEM_CODE.NULL:
                    return "Sound ID";
                case SEM_CODE.SET_LOOP:
                    return "Loop Amt";
                case SEM_CODE.EXECUTE_LOOP:
                    return "Backup Instruction Distance";
                case SEM_CODE.SET_PRIORITY:
                case SEM_CODE.ADD_PRIORITY:
                    return "Priority";
                case SEM_CODE.PLAY:
                case SEM_CODE.PLAY_ADD_VOLUME:
                    return "Volume";
                case SEM_CODE.SET_CHANNEL_BALANCE:
                case SEM_CODE.ADD_CHANNEL_BALANCE:
                    return "Channel Balance";
                case SEM_CODE.SET_UNUSED:
                case SEM_CODE.ADD_UNUSED:
                    return "Unused Value";
                case SEM_CODE.SET_PITCH:
                case SEM_CODE.ADD_PITCH:
                    return "Pitch";
                case SEM_CODE.SET_REVERB1:
                case SEM_CODE.ADD_REVERB1:
                    return "Reverb Value 1";
                case SEM_CODE.SET_REVERB2:
                case SEM_CODE.ADD_REVERB2:
                    return "Reverb Value 2";
                case SEM_CODE.SET_REVERB3:
                    return "Reverb Value 3";
                case SEM_CODE.SET_REVERB4:
                    return "Reverb Value 4";
            }
        }

        public bool HasTimer
        {
            get 
            {
                switch (Code)
                {
                    case SEM_CODE.SET_TIMER:
                    case SEM_CODE.PLAY:
                    case SEM_CODE.PLAY_ADD_VOLUME:
                    case SEM_CODE.SET_CHANNEL_BALANCE:
                    case SEM_CODE.ADD_CHANNEL_BALANCE:
                    case SEM_CODE.SET_UNUSED:
                    case SEM_CODE.ADD_UNUSED:
                    case SEM_CODE.SET_PITCH:
                    case SEM_CODE.ADD_PITCH:
                    case SEM_CODE.SET_REVERB1:
                    case SEM_CODE.ADD_REVERB1:
                    case SEM_CODE.SET_REVERB2:
                    case SEM_CODE.ADD_REVERB2:
                        return true;
                    default:
                        return false;
                    }
                }
        }

        public int MaxTimer
        {
            get
            {
                switch (Code)
                {
                    case SEM_CODE.SET_TIMER:
                        return 0xFFFFFF;
                    case SEM_CODE.PLAY:
                    case SEM_CODE.PLAY_ADD_VOLUME:
                    case SEM_CODE.SET_CHANNEL_BALANCE:
                    case SEM_CODE.ADD_CHANNEL_BALANCE:
                    case SEM_CODE.SET_UNUSED:
                    case SEM_CODE.ADD_UNUSED:
                    case SEM_CODE.SET_REVERB1:
                    case SEM_CODE.ADD_REVERB1:
                    case SEM_CODE.SET_REVERB2:
                    case SEM_CODE.ADD_REVERB2:
                        return ushort.MaxValue;
                    case SEM_CODE.SET_PITCH:
                    case SEM_CODE.ADD_PITCH:
                        return byte.MaxValue;
                    default:
                        return 0;
                }
            }
        }

        public bool HasValue
        {
            get
            {
                switch (Code)
                {
                    case SEM_CODE.SET_TIMER:
                    case SEM_CODE.END_PLAYBACK:
                    case SEM_CODE.LOOP_PLAYBACK:
                        return false;
                    default:
                        return true;
                }
            }
        }

        public int MinValue
        {
            get
            {
                switch (Code)
                {
                    case SEM_CODE.SET_SFXID:
                        return -1;
                    case SEM_CODE.PLAY_ADD_VOLUME:
                    case SEM_CODE.ADD_CHANNEL_BALANCE:
                    case SEM_CODE.SET_CHANNEL_BALANCE:
                    case SEM_CODE.ADD_UNUSED:
                    case SEM_CODE.ADD_REVERB1:
                    case SEM_CODE.ADD_REVERB2:
                    case SEM_CODE.SET_REVERB1:
                    case SEM_CODE.SET_REVERB2:
                        return sbyte.MinValue;
                    case SEM_CODE.SET_PITCH:
                    case SEM_CODE.ADD_PITCH:
                        return short.MinValue;
                    case SEM_CODE.ADD_PRIORITY:
                        return -0x1000000;
                    default:
                        return 0;
                }
            }
        }

        public int MaxValue
        {
            get
            {
                switch (Code)
                {
                    case SEM_CODE.PLAY:
                    case SEM_CODE.PLAY_ADD_VOLUME:
                    case SEM_CODE.SET_CHANNEL_BALANCE:
                    case SEM_CODE.ADD_CHANNEL_BALANCE:
                    case SEM_CODE.SET_UNUSED:
                    case SEM_CODE.ADD_UNUSED:
                    case SEM_CODE.SET_REVERB1:
                    case SEM_CODE.ADD_REVERB1:
                    case SEM_CODE.SET_REVERB2:
                    case SEM_CODE.ADD_REVERB2:
                        return byte.MaxValue;
                    case SEM_CODE.SET_PITCH:
                    case SEM_CODE.ADD_PITCH:
                        return short.MaxValue;
                    case SEM_CODE.SET_SFXID:
                    case SEM_CODE.SET_LOOP:
                    case SEM_CODE.EXECUTE_LOOP:
                    case SEM_CODE.SET_PRIORITY:
                    case SEM_CODE.ADD_PRIORITY:
                    case SEM_CODE.SET_REVERB3:
                    case SEM_CODE.SET_REVERB4:
                    case SEM_CODE.NULL:
                        return 0xFFFFFF;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var value = HasValue ? " = " + Value : "";
            var timer = HasTimer && Timer != 0 ? $" then wait {Timer} ticks" : "";
            return $"{Code}{value}{timer}";
        }
    }

}
