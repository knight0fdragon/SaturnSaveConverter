// SaturnSaveConverter.Program
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SaturnSaveConverter;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Saturn Save Converter. (C) 2022 Knight0fDragon");
            Console.WriteLine("Simple drag and drop utility to convert Saturn save files of many formats");

            Console.WriteLine("usage - SaturnSaveConverter <filename.ext>");
            return;
        }
        byte[] saveBinary = File.ReadAllBytes(args[0]);
        List<byte> verbatumInternal = new List<byte>();
        List<byte> verbatumExternal = new List<byte>();

        void defaultSave()
        {
            var saveFile = new SaveFile();
            using MemoryStream memoryStream = new MemoryStream(saveBinary);
            using BinaryReader br = new BinaryReader(memoryStream);
            saveFile.Name = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(args[0]).PadRight(12, '\0').Substring(0, 12));
            // = 0;
            saveFile.Comment = br.ReadBytes(10);
            //var value = 0x829DBb14u;
            br.ReadBytes(2);
            var value = br.ReadUInt32();
            // saveFile.Language = (byte)(value & 0x7);
            var minutes = (value >> 3) & 0x3F;

            var hours = (value >> 9) & 0x1F;
            var day = (value >> 14) & 0x1F;
            var month = (int)(value >> 19) & 0xF;
            var year = (int)((value >> 23) & 0x1FF) + 1980;

            var testDate = new DateTime().AddYears((int)year < 0 ? 0 : year - 1).AddMonths((int)month < 0 ? 0 : month - 1).AddDays((int)day < 0 ? 0 : day - 1).AddHours((int)hours < 0 ? 0 : hours).AddMinutes((int)minutes < 0 ? 0 : minutes);
            if (testDate < new DateTime(1980, 1, 1)) testDate = new DateTime(1980, 1, 1);
            saveFile.Date = (uint)(Math.Max((testDate - new DateTime(1980, 1, 1)).TotalMinutes, 0));
            //saveFile.FileSize = br.ReadUInt32Flip();
            saveFile.Data = br.ReadToEnd();
            saveFile.FileSize = (uint)saveFile.Data.Length;
            var bup = new BUP(saveFile, 0x40 - 4);
            verbatumInternal = bup.ExtractInternal().ToList();
            bup = new BUP(saveFile, 0x200);
            verbatumExternal = bup.ExtractExternal().ToList();
        }
        try
        {
            switch ((char)saveBinary[0])
            {
                case '\0':
                case (char)0xFF:
                    {
                        var verbatum = new List<byte>();
                        for (int i = 1; i < saveBinary.Length; i += 2)
                        {
                            verbatum.Add(saveBinary[i]);

                        }
                        var blockSize = BUP.SectorSize(verbatum.ToArray());
                        if (blockSize == 0x40)
                            verbatumInternal.AddRange(verbatum);
                        else
                            verbatumExternal.AddRange(verbatum);
                        break;
                    }

                case 'V':
                    if ((char)saveBinary[1] == 'm' && (char)saveBinary[2] == 'e' && (char)saveBinary[3] == 'm' && (char)saveBinary[4] == '\0')
                    {
                        verbatumInternal = BUP.ExtractInternal(saveBinary).ToList();
                        verbatumExternal = BUP.ExtractExternal(saveBinary).ToList();
                    }
                    else
                    {
                        defaultSave();
                    }
                    break;


                case 'B':

                    {
                        bool isBackup = false;
                        try
                        {
                            isBackup = (Encoding.UTF8.GetString(saveBinary.Take(16).ToArray()) == "BackUpRam Format");
                        }
                        catch { }
                        if (isBackup)
                        {
                            var blockSize = BUP.SectorSize(saveBinary.ToArray());

                            if (blockSize == 0x40)
                                verbatumInternal = saveBinary.ToList();

                            else
                                verbatumExternal = saveBinary.ToList();


                        }

                    }
                    break;
                default:
                    {
                        defaultSave();
                    }
                    //verbatum = $"Vmem\0\0\0\0\0\0\0\0\0\0\0\0{Path.GetFileNameWithoutExtension(args[0]).PadRight(12, '\0').Substring(0, 12)}".Cast<byte>().Concat(saveBinary).ToList();
                    break;
            }
        }
        catch
        {
            defaultSave();
        }

        try
        {

            string name = Path.GetFileNameWithoutExtension(args[0]);

            if (verbatumInternal.Count == 0) verbatumInternal = Switch(verbatumExternal.ToArray());
            if (verbatumExternal.Count == 0) verbatumExternal = Switch(verbatumInternal.ToArray());

            
            Output(name, verbatumInternal.ToArray(),"Internal", Path.Combine(Directory.GetCurrentDirectory(), "Saves", name));
            Output(name, verbatumExternal.ToArray(), "4MB", Path.Combine(Directory.GetCurrentDirectory(), "Saves", name));

            var blockSize = BUP.SectorSize(verbatumExternal.ToArray());

            var individualFiles = SeparateSaveFiles(verbatumExternal.ToArray(), blockSize);
            blockSize = BUP.SectorSize(verbatumInternal.ToArray());

            var idx = 1;

            var bupPath = Path.Combine(Directory.GetCurrentDirectory(), "Saves", name, "BUP");
            Directory.CreateDirectory(bupPath);

            foreach (var file in individualFiles)
            {
                var filename = Encoding.UTF8.GetString(file.Name);
                //File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "Saves", "RAW_" + Path.ChangeExtension(filename, "raw")));
                //File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Saves", "RAW_" + Path.ChangeExtension(filename, "raw")), file.Data);

                file.Save(Path.Combine(Directory.GetCurrentDirectory(), "Saves", name, "SSF", "HOOKS"), idx);
                var bup = new BUP(file, blockSize);

                var extracted = (blockSize == 0x40) ? bup.ExtractInternal() : bup.ExtractExternal();
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Saves", name, "Singles", filename));

                Output(filename, extracted,"", Path.Combine(Directory.GetCurrentDirectory(), "Saves", name, "Singles", filename));
                bup.Save(bupPath, idx++);

                var rawPath =  Path.Combine(Directory.GetCurrentDirectory(), "Saves", name, "Raw");
                Directory.CreateDirectory(rawPath);

                File.Delete(Path.Combine(rawPath,  Path.ChangeExtension(filename, "raw")));
                File.WriteAllBytes(Path.Combine(rawPath,  Path.ChangeExtension(filename, "raw")), file.Data);

            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Not a valid save file");
            throw;
        }
        Console.WriteLine("Save conversion successful");

    }
    static List<byte> Switch(byte[] verbatum)
    {
        var sectorSize = BUP.SectorSize(verbatum);
        var individualFiles = SeparateSaveFiles(verbatum, sectorSize);
        var output = new List<byte>();
        var newSectorSize = (ushort)((sectorSize == 0x40) ? 0x200 : 0x40);
        
        foreach (var file in individualFiles)
        {
            var bup = new BUP(file, newSectorSize);

            var currentSector = (ushort)(Math.Ceiling((double)output.Count / newSectorSize));
            var data = (newSectorSize == 0x40) ? bup.ExtractInternal(currentSector,false) : bup.ExtractExternal(currentSector,false);
            
            //data = data.Reverse().SkipWhile(d => d == 0).Reverse().ToArray();
            data = data.Concat(new byte[(newSectorSize - data.Length % newSectorSize ) % newSectorSize]).ToArray();

            output.AddRange(data);

        }
        return output.Concat(new byte[newSectorSize]).Take(newSectorSize).ToList();

    }
    static void Output(string name, byte[] verbatum,string locationName,string currentPath)
    {
        
        
      
   
        if (verbatum.Length == 0) return;
        var blockSize = BUP.SectorSize(verbatum);
        var path = "";
   
        var ssfPath = path = Path.Combine(currentPath,  "SSF", locationName);
        Directory.CreateDirectory(path);
        var mednafenPath = path = Path.Combine(currentPath,  "Mednafen", locationName);
        Directory.CreateDirectory(path);
        var beetlePath = path = Path.Combine(currentPath,  "Beetle", locationName);
        Directory.CreateDirectory(path);
        var giriPath = path = Path.Combine(currentPath,  "Girigiri", locationName);
        Directory.CreateDirectory(path);

        var yabasanshiroPath = path = Path.Combine(currentPath,  "Yabasanshiro", locationName);
        Directory.CreateDirectory(path);
        if(locationName == "Internal")            Directory.CreateDirectory(Path.Combine(path, "Expanded"));

        var yabausePath = path = Path.Combine(currentPath,  "Yabause", locationName);
        Directory.CreateDirectory(path);

        var kronosPath = path = Path.Combine(currentPath,  "Kronos", locationName);
        Directory.CreateDirectory(path);

        if( !string.IsNullOrEmpty(locationName) && locationName != "Internal")
        {
            foreach (var cname in new[] { "4", "8", "16", "32" })
            {
                Directory.CreateDirectory(kronosPath.Replace($"4MB", $"{cname}MB"));
                Directory.CreateDirectory(yabausePath.Replace($"4MB", $"{cname}MB"));
                Directory.CreateDirectory(yabasanshiroPath.Replace($"4MB", $"{cname}MB"));

            }
        }
        var saturnPath = path = Path.Combine(currentPath,  "Saturn", locationName);
        Directory.CreateDirectory(path);
        var novaPath = path = Path.Combine(currentPath,  "Nova", locationName);
        Directory.CreateDirectory(path);


        var verbatumFixed = verbatum.Concat(new byte[blockSize == 0x40 ? 0x8000 : 0x80000]).Take(blockSize == 0x40 ? 0x8000 : 0x80000).ToArray();

        List<byte> offStartBytes = new List<byte>();
        List<byte> onStartBytes = new List<byte>();
        
        for (int j = 0; j < verbatum.Length; j++)
        {
            offStartBytes.Add(0);
            offStartBytes.Add(verbatumFixed[j]);
            onStartBytes.Add(byte.MaxValue);
            onStartBytes.Add(verbatumFixed[j]);
        }




        File.Delete(Path.Combine(mednafenPath, Path.ChangeExtension(name, string.IsNullOrEmpty(locationName) || locationName == "Internal" ? "bkr" : "bcr" ))); ;
        File.Delete(Path.Combine(beetlePath, Path.ChangeExtension(name, string.IsNullOrEmpty(locationName) || locationName == "Internal" ? "bkr" : "bcr")));
        File.Delete(Path.Combine(ssfPath, Path.ChangeExtension(name, "bin")));
        File.Delete(Path.Combine(saturnPath, Path.ChangeExtension(name, "raw")));
        File.Delete(Path.Combine(giriPath, Path.ChangeExtension(name, "bin")));
        File.Delete(Path.Combine(yabausePath, Path.ChangeExtension(name, "bin")));
        File.Delete(Path.Combine(kronosPath, Path.ChangeExtension(name, "bin")));
        File.Delete(Path.Combine(yabasanshiroPath, Path.ChangeExtension(name, "bin")));
        File.Delete(Path.Combine(novaPath, Path.ChangeExtension(name, "bup")));

        if (locationName != "Internal")
        {
            foreach (var cname in new[] { 4, 8, 16, 32 })
            {
                File.Delete(Path.Combine(kronosPath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")));
                File.Delete(Path.Combine(yabausePath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")));
                File.Delete(Path.Combine(yabasanshiroPath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")));
            }
        }

        if (verbatum.Length <= verbatumFixed.Length)
        {
            File.WriteAllBytes(Path.Combine(mednafenPath, Path.ChangeExtension(name, string.IsNullOrEmpty(locationName) || locationName == "Internal" ? "bkr" : "bcr")), verbatumFixed.ToArray());
            File.WriteAllBytes(Path.Combine(beetlePath, Path.ChangeExtension(name, string.IsNullOrEmpty(locationName) || locationName == "Internal" ? "bkr" : "bcr")), verbatumFixed.ToArray());
            File.WriteAllBytes(Path.Combine(ssfPath, Path.ChangeExtension(name, "bin")), offStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(saturnPath, Path.ChangeExtension(name, "raw")), offStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(giriPath, Path.ChangeExtension(name, "bin")), onStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(yabausePath, Path.ChangeExtension(name, "bin")), onStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(kronosPath, Path.ChangeExtension(name, "bin")), onStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(yabasanshiroPath, Path.ChangeExtension(name, "bin")), onStartBytes.ToArray());
            File.WriteAllBytes(Path.Combine(novaPath, Path.ChangeExtension(name, "bup")), verbatumFixed.ToArray());
        

            if (locationName != "Internal")
            {
                foreach (var cname in new[] { 4, 8, 16, 32 })
                {
                    if (verbatum.Length <= 0x20000 * cname)
                    {
                        File.WriteAllBytes(Path.Combine(kronosPath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")), onStartBytes.Concat(new byte[0x40000 * cname]).Take(0x40000 * cname).ToArray());
                        File.WriteAllBytes(Path.Combine(yabausePath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")), onStartBytes.Concat(new byte[0x40000 * cname]).Take(0x40000 * cname).ToArray());
                        File.WriteAllBytes(Path.Combine(yabasanshiroPath.Replace($"4MB", $"{cname}MB"), Path.ChangeExtension(name, "bin")), onStartBytes.Concat(new byte[0x40000 * cname]).Take(0x40000 * cname).ToArray());
                    }
                }
            }
        }

        if ((locationName == "Internal"))
        {
            var verbatumExpanded = verbatum.Concat(new byte[0x400000]).Take(0x400000).ToArray();
            List<byte> onStartBytesExpanded = new List<byte>();
            for (int j = 0; j < verbatumExpanded.Length; j++)
            {

                onStartBytesExpanded.Add(byte.MaxValue);
                onStartBytesExpanded.Add(verbatumExpanded[j]);
            }
            File.Delete(Path.Combine(yabasanshiroPath, "Expanded", Path.ChangeExtension(name, "bin")));
            File.WriteAllBytes(Path.Combine(yabasanshiroPath, "Expanded" , Path.ChangeExtension(name, "bin")), onStartBytesExpanded.ToArray());
        }
    }

    static List<SaveFile> SeparateSaveFiles(byte[] verbatum, int blockSize)
    {
        List<int> saveFileSectors = new List<int>();
        List<SaveFile> saveFiles = new List<SaveFile>();
        for (int sector = 0; sector < verbatum.Length / blockSize; sector++)
        {
            if (verbatum.Skip(sector * blockSize).Take(1).First() != 0x80) continue;
            saveFileSectors.Add(sector);
        }


        foreach (var sector in saveFileSectors)
        {
            SaveFile saveFile = new SaveFile();
            var chunk = verbatum.Skip(sector * blockSize + 4).Take(blockSize - 4).ToArray();
            using MemoryStream memoryStream = new MemoryStream(chunk);
            using BinaryReader br = new BinaryReader(memoryStream);
            saveFile.Name = br.ReadBytes(11);
            br.ReadByte();
            saveFile.Comment = br.ReadBytes(10);


            saveFile.Date = br.ReadUInt32Flip();

            saveFile.FileSize = br.ReadUInt32Flip();
            if ((int)saveFile.FileSize <= 0)
            {
                Console.WriteLine("File size in save invalid. Skipping.");
                continue;
            }
            var extraSectors = new List<ushort>();

            while (true)
            {
                var sectorNum = br.ReadUInt16Flip();
                if (sectorNum == 0) break;
                extraSectors.Add(sectorNum);
                if (br.Length() == br.Position())
                {
                    var newChunk = verbatum.Skip(extraSectors.First() * blockSize + 4).Take(blockSize - 4).ToArray();
                    Array.Copy(newChunk, chunk, 60);
                    extraSectors.RemoveAt(0);
                    br.Position(0);
                }
            }
            var data = br.ReadToEnd().ToList();

            foreach (var eSector in extraSectors)
            {
                data.AddRange(verbatum.Skip(eSector * blockSize + 4).Take(blockSize - 4));
            }
            
            saveFile.Data = data.Take((int)(saveFile.FileSize)).ToArray();// + 4 is checksum
            saveFiles.Add(saveFile);


        }
        return saveFiles;
    }
}
