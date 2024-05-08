﻿namespace FileManager.Models
{
    public class DriveItem : DirectoryItem
    {
        public long FreeSize { get; set; }
        public long TotalSize { get; set; }

        public DriveItem(string fileName, string filePath, short side, long space, string info) : base(fileName, filePath, 0, side, false, ItemType.Drive)
        {
            Type = ItemType.Drive;
            FileInfo = info;
            Size = space;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
