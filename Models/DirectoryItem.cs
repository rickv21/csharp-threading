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

        public DirectoryItem(string fileName, string filePath, int itemCount) : base(fileName, filePath)
        {
            ItemCount = itemCount;
            Type = FileType.Dir;
        }
    }
}
