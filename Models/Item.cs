using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public abstract class Item
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Date { get; set; }
        public ImageSource Icon { get; set; }
        public ItemType Type { get; set; } //Make it only settable internally.
        public string FileInfo { get; set; }
        public long Size { get; set; }
        public string ReadableSize { get; set; }
        public short Side {  get; }

        public Item(string fileName, string filePath, ImageSource icon, short side, Boolean hidden) 
        {
            FileName = fileName;
            FilePath = filePath;
            Side = side;
            Date = ""; //Temp
            Size = 0;
            Icon = icon;
            FileInfo = hidden ? "(Hidden)" : "";
        }

        public override string ToString()
        {
            return $"FileName: {FileName}, ItemType: {Type}, FilePath: {FilePath}, Date: {Date}, FileType: {FileInfo}, Size: {Size}";
        }
    }
}
