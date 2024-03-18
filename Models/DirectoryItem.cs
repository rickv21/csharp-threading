using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class DirectoryItem : Item
    {
        public int ItemCount { get; set; }

        public DirectoryItem(string fileName, string filePath, int itemCount, ImageSource icon) : base(fileName, filePath, icon)
        {
            ItemCount = itemCount;
            Type = FileType.Dir;
        }
    }
}
