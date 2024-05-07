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

        public DirectoryItem(string fileName, string filePath, int itemCount, short side, Boolean hidden, ItemType type = ItemType.Dir) : base(fileName, filePath, type == ItemType.Drive ? "drive_icon.png" : "folder_icon.png", side, hidden)
        {
            ItemCount = itemCount;
            Type = type;
            LastEdited = null;
        }

        public override string ToString()
        {
            return base.ToString() + $", ItemCount: {ItemCount}";
        }
    }
}
