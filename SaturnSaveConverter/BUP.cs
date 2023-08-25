// SaturnSaveConverter.BUP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SaturnSaveConverter
{

    internal class BUP
    {
        static public int SectorSize(byte[] data)
        {
            var sectorLine = 0;
            try
            {


                while (Encoding.UTF8.GetString(data.Skip(sectorLine * 16).Take(16).ToArray()) == "BackUpRam Format")
                {
                    System.Diagnostics.Debug.Print(Encoding.UTF8.GetString(data.Skip(sectorLine * 16).Take(16).ToArray()));
                    sectorLine++;
                }
                return sectorLine * 16;
            }
            catch { }
            return 0x40;
        }
        private byte[] Header;
        private byte[] SaveID;
        private byte[] Stats;
        private byte[] Unused;
        private byte[] Filename;
        private byte[] Comment;
        private byte Language;
        private DateTime Date;
        private uint FileLength;
        private ushort NumberOfBlocks;
        private ushort Padding;
        private DateTime Date2;
        private ulong Reserved;
        private byte[] Data;

        private BUP(byte[] data)
        {
            using MemoryStream input = new MemoryStream(data);
            using BinaryReader binaryReader = new BinaryReader(input);
            Header = binaryReader.ReadBytes(4);
            SaveID = binaryReader.ReadBytes(4);
            Unused = binaryReader.ReadBytes(4);
            Header = binaryReader.ReadBytes(4);
            Filename = binaryReader.ReadBytes(11);
            binaryReader.ReadByte();
            Comment = binaryReader.ReadBytes(10);
            binaryReader.ReadByte();
            Language = binaryReader.ReadByte();
            uint num = binaryReader.ReadUInt32Flip();
            Date = new DateTime(1980, 1, 1).AddMinutes((int)num < 0 ? 0 : num);
            FileLength = binaryReader.ReadUInt32Flip();
            NumberOfBlocks = binaryReader.ReadUInt16Flip();
            Padding = binaryReader.ReadUInt16Flip();
            num = binaryReader.ReadUInt32Flip();
            Date2 = new DateTime(1980, 1, 1).AddMinutes((int)num < 0 ? 0 : num);
            Reserved = binaryReader.ReadUInt64Flip();
            Data = binaryReader.ReadToEnd();
        }

        public BUP(SaveFile file, int blockSize)
        {
            blockSize -= 4;
            Header = new byte[] { 0x56, 0x6D, 0x65, 0x6D };
            SaveID = new byte[4];
            Stats = new byte[4];
            Unused = new byte[4];

            Data = file.Data;
            Filename = file.Name.Concat(new byte[11]).Take(11).ToArray();
            Comment = file.Comment.Concat(new byte[10]).Take(10).ToArray();
            Date = new DateTime(1980, 1, 1).AddMinutes((int)file.Date < 0 ? 0 : file.Date);
            Date2 = new DateTime(1980, 1, 1).AddMinutes((int)file.Date < 0 ? 0 : file.Date);

            
            var numberOfGameBlocks = (int)Math.Ceiling(Data.Length / (float)(blockSize));

            var freespace = numberOfGameBlocks * blockSize - Data.Length;

            freespace -= 0x1E; //size of header

            freespace -= numberOfGameBlocks * 2;
            if(freespace < 0)
            {
                var bytesNeeded = Math.Abs(freespace);
                var numberOfadditionalBlocks = (int)Math.Ceiling(bytesNeeded / (float)(blockSize));
                numberOfGameBlocks += numberOfadditionalBlocks;
            }
            
            

            NumberOfBlocks = (ushort)numberOfGameBlocks;

            Language = file.Language;
            FileLength = (uint)file.Data.Length;
        }



        public void Save(string path, int index)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(Header);
            bw.Write(SaveID);
            bw.Write(Stats);
            bw.Write(Unused);
            bw.Write(Filename);
            bw.Write((byte)0x00);
            bw.Write(Comment);
            bw.Write((byte)0x00);
            bw.Write(Language);
            if (Date < new DateTime(1980, 1, 1)) Date = new DateTime(1980, 1, 1);
            bw.WriteFlip(Math.Max((int)(Date - new DateTime(1980, 1, 1)).TotalMinutes, 0));
            bw.WriteFlip(FileLength);
            bw.WriteFlip(NumberOfBlocks);
            bw.WriteFlip(Padding);
            if (Date2 < new DateTime(1980, 1, 1)) Date2 = new DateTime(1980, 1, 1);
            bw.WriteFlip(Math.Max((int)(Date2 - new DateTime(1980, 1, 1)).TotalMinutes, 0));
            bw.WriteFlip(Reserved);
            bw.Write(Data);

            var output = ms.ToArray();
            File.Delete(Path.Combine(path, $"{Encoding.UTF8.GetString(Filename)}.BUP"));

            File.WriteAllBytes(Path.Combine(path, $"{Encoding.UTF8.GetString(Filename)}.BUP"), output);
        }

        public byte[] Extract(int blockSize, int cartSize, ushort sectorStart = 0,  bool pad = true)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            if (sectorStart == 0)
            {
                sectorStart += 2;
                for (int i = 0; i < blockSize / 16; i++)
                {
                    binaryWriter.Write("BackUpRam Format".Select((char x) => (byte)x).ToArray());
                }
                binaryWriter.Write(Enumerable.Repeat((byte)0, blockSize).ToArray());
            }

            binaryWriter.WriteFlip(0x80000000);

            binaryWriter.Write(Filename);
            binaryWriter.Write((byte)0);
            //binaryWriter.Write(Language);
            binaryWriter.Write(Comment);
            if (Date < new DateTime(1980, 1, 1)) Date = new DateTime(1980, 1, 1);
            binaryWriter.WriteFlip((int)(Math.Max((Date - new DateTime(1980, 1, 1)).TotalMinutes, 0)));
            binaryWriter.WriteFlip(FileLength);
            if ((binaryWriter.BaseStream.Position & 1) == 1)
            {
                throw new Exception("File not aligned by 2 bytes");
            }

            if ((binaryWriter.BaseStream.Position % blockSize) == 0L)
            {
                binaryWriter.Write(0);
            }


            for (ushort num = (ushort)(sectorStart + 1); num < sectorStart + NumberOfBlocks; num++)
            {
                binaryWriter.WriteFlip(num);


                if ((binaryWriter.BaseStream.Position % blockSize) == 0L)
                {
                    binaryWriter.Write(0);
                }
            }

            binaryWriter.Write((short)0);

            byte[] data2 = Data;
            foreach (byte value in data2)
            {
                if ((binaryWriter.BaseStream.Position % blockSize) == 0L)
                {
                    binaryWriter.Write(0);
                }
                binaryWriter.Write(value);
            }

            if (pad)
            {
                if (binaryWriter.BaseStream.Position % blockSize != 0)
                {
                    binaryWriter.Write(Enumerable.Repeat((byte)0, (int)((blockSize - (binaryWriter.BaseStream.Position % blockSize) % blockSize))).ToArray());
                }
                if (binaryWriter.BaseStream.Position < cartSize)
                {

                    binaryWriter.Write(Enumerable.Repeat((byte)0, (int)(cartSize - binaryWriter.BaseStream.Position)).ToArray());
                }
            }
            var mem = memoryStream.ToArray();

            return mem;
        }

        public byte[] ExtractInternal(ushort sectorStart = 0, bool pad = true) => Extract(0x40, 0x8000, sectorStart,pad);
        public byte[] ExtractExternal(ushort sectorStart = 0, bool pad = true) => Extract(0x200, 0x80000, sectorStart, pad);


        public static byte[] ExtractInternal(byte[] data, ushort sectorStart = 0,bool pad = true)
        {
            BUP bUP = new BUP(data);
            return bUP.ExtractInternal(sectorStart, pad);
        }
        public static byte[] ExtractExternal(byte[] data, ushort sectorStart = 0, bool pad = true)
        {
            BUP bUP = new BUP(data);
            return bUP.ExtractExternal(sectorStart, pad);
        }




    }
}