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
        public FileType Type { get; set; } //Make it only settable internally.

        public Item(string fileName, string filePath, ImageSource icon) 
        {
            FileName = fileName;
            FilePath = filePath;
            Date = ""; //Temp
            Icon = icon;
        }


    }
}
