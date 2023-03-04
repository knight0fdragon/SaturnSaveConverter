using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SaturnSaveConverter
{
    public static class Extensions
    {
        public static byte[] ReadToEnd(this BinaryReader br) => br.ReadBytes((int)(br.Length() - br.Position()));
        public static short ReadInt16Flip(this BinaryReader br) => br.ReadInt16().FlipEndian();
        public static int ReadInt32Flip(this BinaryReader br) => br.ReadInt32().FlipEndian();
        public static long ReadInt64Flip(this BinaryReader br) => br.ReadInt64().FlipEndian();
        public static ushort ReadUInt16Flip(this BinaryReader br) => br.ReadUInt16().FlipEndian();
        public static uint ReadUInt32Flip(this BinaryReader br) => br.ReadUInt32().FlipEndian();
        public static ulong ReadUInt64Flip(this BinaryReader br) => br.ReadUInt64().FlipEndian();
        public static void WriteFlip(this BinaryWriter bw, ulong value) => bw.Write(value.FlipEndian());
        public static void WriteFlip(this BinaryWriter bw, uint value) => bw.Write(value.FlipEndian());
        public static void WriteFlip(this BinaryWriter bw, ushort value) => bw.Write(value.FlipEndian());
        public static void WriteFlip(this BinaryWriter bw, long value) => bw.Write(value.FlipEndian());
        public static void WriteFlip(this BinaryWriter bw, int value) => bw.Write(value.FlipEndian());
        public static void WriteFlip(this BinaryWriter bw, short value) => bw.Write(value.FlipEndian());
        public static short FlipEndian(this short value) => (short)FlipEndian((ushort)value);
        public static ushort FlipEndian(this ushort value) => (ushort)(((value & 0xFF00) >> 8) | ((value & 0xFF) << 8));
        public static int FlipEndian(this int value) => (int)FlipEndian((uint)value);
        public static uint FlipEndian(this uint value) => ((value & 0xFF0000) >> 8) | ((value & 0xFF00) << 8) | ((value & 0xFF000000) >> 24) | ((value & 0xFF) << 24);
        public static long FlipEndian(this long value) => (long)FlipEndian((ulong)value);
        public static ulong FlipEndian(this ulong value) =>
            ((value & 0xFF00000000000000) >> 56) |
            ((value & 0xFF000000000000) >> 40) |
            ((value & 0xFF0000000000) >> 24) |
            ((value & 0xFF00000000) >> 8) |
            ((value & 0xFF000000) << 8) |
            ((value & 0xFF0000) << 24) |
            ((value & 0xFF00) << 40) |
            ((value & 0xFF) << 56);

        public static void Position(this BinaryReader br, long value) => br.BaseStream.Position = value;
        public static long Position(this BinaryReader br) => br.BaseStream.Position;
        public static long Length(this BinaryReader br) => br.BaseStream.Length;
        public static void Position(this BinaryWriter br, long value) => br.BaseStream.Position = value;
        public static long Position(this BinaryWriter br) => br.BaseStream.Position;
        public static long Length(this BinaryWriter br) => br.BaseStream.Length;
        public static byte[] ToByteArray(this short[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this ushort[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this List<short> buffer) => buffer.ToArray().ToByteArray();
        public static byte[] ToByteArray(this List<ushort> buffer) => buffer.ToArray().ToByteArray();

        public static byte[] ToByteArray(this int[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 24), (byte)(b >> 16), (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this uint[] buffer) => buffer.SelectMany(b => new[] { (byte)(b >> 24), (byte)(b >> 16), (byte)(b >> 8), (byte)b }).ToArray();

        public static byte[] ToByteArray(this List<int> buffer) => buffer.ToArray().ToByteArray();
        public static byte[] ToByteArray(this List<uint> buffer) => buffer.ToArray().ToByteArray();


    }
}
