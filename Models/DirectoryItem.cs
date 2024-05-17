namespace FileManager.Models
{
    public class DirectoryItem : Item
    {
        public int ItemCount { get; set; }

        public DirectoryItem()
        {

        }

        public DirectoryItem(string fileName, string filePath, int itemCount, short side, bool hidden, bool isLink, DateTime? lastEdited, ItemType type = ItemType.Dir) : base(fileName, filePath, type == ItemType.Drive ? "drive_icon.png" : "folder_icon.png", side, hidden, isLink)
        {
            ItemCount = itemCount;
            Type = type;
            LastEdited = lastEdited;
        }

        public override string ToString()
        {
            return base.ToString() + $", ItemCount: {ItemCount}";
        }
    }
}
