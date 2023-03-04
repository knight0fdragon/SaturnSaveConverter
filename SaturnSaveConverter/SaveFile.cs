using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SaturnSaveConverter
{
    public struct SaveFile
    {
        public byte[] Name;
        public byte Language;
        public byte[] Comment;
        public uint Date;
        public uint FileSize;
        public byte[] Data;

        public void Save(string path,int index)
        {

            var comment = new byte[10];
            Array.Copy(Comment, comment, Math.Min(10, Comment.Length));
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(comment);
            bw.Write((ushort)0);

            var date = new DateTime(1980, 1, 1).AddMinutes(((int)Date <= -1) ? 0 : Date);

            var language = (1) & 0x7;
            var minutes = date.Minute & 0x3F;
            var hour = date.Hour & 0x1F;
            var day = date.Day & 0x1F;
            var month = date.Month & 0xF;
            var year = (date.Year - 1980) & 0x1FF;

            var value = year << 23 | month << 19 | day << 14 | hour << 9 | minutes << 3 | language;
            bw.Write(value);

            bw.Write(Data);
            var output = ms.ToArray();
            Directory.CreateDirectory(path);
            File.Delete(Path.Combine(path, $"{Encoding.UTF8.GetString(Name)}.bin"));
            File.WriteAllBytes(Path.Combine(path, $"{Encoding.UTF8.GetString(Name)}.bin"), output);
        }

    }
}
