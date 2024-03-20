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

        public FileItem(string fileName, string filePath, string size, String type, ImageSource icon, short side, Boolean hidden) : base(fileName, filePath, icon, side, hidden)
        {
            Size = size;
            FileInfo = type + " " + FileInfo;
            Type = ItemType.File;
        }
    }
}
