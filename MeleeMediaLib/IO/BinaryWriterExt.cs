﻿using System;
using System.IO;

namespace MeleeMedia.IO
{

    public class BinaryWriterExt : BinaryWriter
    {
        public bool BigEndian { get; set; } = false;

        public long Length { get => BaseStream.Length; }

        public BinaryWriterExt(Stream stream) : base(stream)
        {
        }

        public void Align(int alignment)
        {
            if (BaseStream.Position % alignment > 0)
            {
                Write(new byte[alignment - (BaseStream.Position % alignment)]);
            }
        }

        public override void Write(short s)
        {
            Write(Reverse(BitConverter.GetBytes(s)));
        }

        public override void Write(ushort s)
        {
            Write(Reverse(BitConverter.GetBytes(s)));
        }

        public override void Write(int s)
        {
            Write(Reverse(BitConverter.GetBytes(s)));
        }

        public override void Write(uint s)
        {
            Write(Reverse(BitConverter.GetBytes(s)));
        }

        public override void Write(float s)
        {
            Write(Reverse(BitConverter.GetBytes(s)));
        }

        public override void Write(string s)
        {
            var chars = s.ToCharArray();

            foreach (var v in chars)
                Write((byte)v);

            Write((byte)0);
        }

        public void Seek(uint Position)
        {
            BaseStream.Seek(Position, SeekOrigin.Begin);
        }

        private byte[] Reverse(byte[] b)
        {
            if (BitConverter.IsLittleEndian && BigEndian)
                Array.Reverse(b);
            return b;
        }

        public void PrintPosition()
        {
            Console.WriteLine("Stream at 0x{0}", BaseStream.Position.ToString("X"));
        }


        public void WritePacked(int i)
        {
            if (i > 0xFF || (i & 0x80) > 0)
            {
                Write((byte)((i & 0x7F) | 0x80));
                Write((byte)(i >> 7));
            }
            else
            {
                Write((byte)i);
            }
        }

    }
}
