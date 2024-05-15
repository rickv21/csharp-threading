namespace FileManager.Models
{
    public class FileItem : Item
    {

        public FileItem(string fileName, string filePath, long size, String type, ImageSource icon, short side, Boolean hidden, DateTime lastEditDateTime) : base(fileName, filePath, icon, side, hidden)
        {
            Size = size;
            ReadableSize = FileUtil.ConvertBytesToHumanReadable(Size);
            LastEdited = lastEditDateTime;
            FileInfo = type + " " + FileInfo;
            Type = ItemType.File;
        }
    }
}
