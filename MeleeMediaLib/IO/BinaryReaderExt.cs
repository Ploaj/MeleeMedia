using System;
using System.IO;
using System.Text;

namespace MeleeMedia.IO
{
    public class BinaryReaderExt : BinaryReader
    {
        public bool BigEndian { get; set; } = false;

        public long Length { get => BaseStream.Length; }

        public BinaryReaderExt(Stream stream) : base(stream)
        {
        }

        public override Int16 ReadInt16()
        {
            return BitConverter.ToInt16(Reverse(base.ReadBytes(2)), 0);
        }

        public override UInt16 ReadUInt16()
        {
            return BitConverter.ToUInt16(Reverse(base.ReadBytes(2)), 0);
        }

        public override Int32 ReadInt32()
        {
            return BitConverter.ToInt32(Reverse(base.ReadBytes(4)), 0);
        }

        public override UInt32 ReadUInt32()
        {
            return BitConverter.ToUInt32(Reverse(base.ReadBytes(4)), 0);
        }

        public override float ReadSingle()
        {
            return BitConverter.ToSingle(Reverse(base.ReadBytes(4)), 0);
        }

        public void Skip(uint Size)
        {
            BaseStream.Seek(Size, SeekOrigin.Current);
        }

        public void Seek(uint Position)
        {
            BaseStream.Seek(Position, SeekOrigin.Begin);
        }

        public byte[] Reverse(byte[] b)
        {
            if (BitConverter.IsLittleEndian && BigEndian)
                Array.Reverse(b);
            return b;
        }

        public override string ReadString()
        {
            string str = "";
            byte ch;
            while ((ch = ReadByte()) != 0)
                str += (char)ch;
            return str;
        }

        public string ReadString(int Size)
        {
            string str = "";
            for (int i = 0; i < Size; i++)
            {
                byte b = ReadByte();
                if (b != 0)
                {
                    str += (char)b;
                }
            }
            return str;
        }
        
        public uint Position
        {
            get { return (uint)BaseStream.Position; }
            set
            {
                BaseStream.Position = value;
            }
        }

        public void WriteInt32At(int Value, int Position)
        {
            byte[] data = Reverse(BitConverter.GetBytes(Value));
            long temp = BaseStream.Position;
            BaseStream.Position = Position;
            BaseStream.Write(data, 0, data.Length);
            BaseStream.Position = temp;
        }

        public byte[] GetStreamData()
        {
            long temp = Position;
            Seek(0);
            byte[] data = ReadBytes((int)BaseStream.Length);
            Seek((uint)temp);
            return data;
        }

        public byte[] GetSection(uint Offset, int Size)
        {
            long temp = Position;
            Seek(Offset);
            byte[] data = ReadBytes(Size);
            Seek((uint)temp);
            return data;
        }

        internal string ReadString(uint offset, int size)
        {
            string str = "";

            var temp = Position;
            Position = offset;

            if (size == -1)
            {
                byte b = ReadByte();
                while (b != 0)
                {
                    str += (char)b;
                    b = ReadByte();
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    byte b = ReadByte();
                    if (b != 0)
                    {
                        str += (char)b;
                    }
                }
            }

            Position = temp;

            return str;
        }
        

    }
}
