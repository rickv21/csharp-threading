using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FileManager.Models
{
    public class FileItem : Item
    {
        public double Size { get; set; }
        public string FileExtension { get; set; }

        public FileItem(string fileName, string filePath, double size, String type, ImageSource icon) : base(fileName, filePath, icon)
        {
            Size = size;
            FileExtension = type;
            Type = FileType.File;
        }
    }
}
