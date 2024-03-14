using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class FileItem : Item
    {
        public int Size { get; set; }
        public string FileExtension { get; set; }

        public FileItem(string fileName, string filePath, int size, String type) : base(fileName, filePath)
        {
            Size = size;
            FileExtension = type;
            Type = FileType.File;
        }
    }
}
