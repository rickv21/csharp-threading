﻿namespace FileManager.Models
{
    public class DriveItem : DirectoryItem
    {
        public long FreeSize { get; set; }
        public long TotalSize { get; set; }

        public DriveItem(string fileName, string filePath, short side, string space, string info, DateTime? lastEdited) : base(fileName, filePath, 0, side, false, false, lastEdited, ItemType.Drive)
        {
            Type = ItemType.Drive;
            FileInfo = info;
            ReadableSize = space;
            LastEdited = lastEdited;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
