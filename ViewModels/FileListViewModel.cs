using FileManager.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileManager.ViewModels
{
    public class FileListViewModel : ViewModelBase
    {
        private ObservableCollection<Item> _files;
        private String _currentPath;
        private String previousPath;
        private ConcurrentDictionary<string, byte[]> _fileIconCache;
        private readonly short side;
        private IList<object> _selectedItems;

        public ObservableCollection<Item> Files
        {
            get { return _files; }
            set
            {
                _files = value;
            }
        }

        public IList<object> SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
            }
        }

        public String CurrentPath
        {
            get { return _currentPath; }
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
                // PathChanged(value);
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ICommand ItemDoubleTappedCommand { get; }
        public ICommand PathChangedCommand { get; }


        public FileListViewModel(ConcurrentDictionary<string, byte[]> fileIconCache, short side)
        {
            this.side = side;
            _files = new ObservableCollection<Item>();
            _fileIconCache = fileIconCache;
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DirectoryInfo d = new DirectoryInfo(_currentPath);
            SelectedItems = new ObservableCollection<object>();
            ItemDoubleTappedCommand = new Command<Item>(OpenItem);
            PathChangedCommand = new Command<string>(PathChanged);

            Task.Run(() => FillList(d));
        }

        public void HandleClick(string key)
        {
            switch (key)
            {
                case "f5":
                    //Refresh
                    Refresh();
                    return;
                case "f6":
                    //Copy
                    Debug.WriteLine("Copy item.");
                    return;
                case "f7":
                    //Move
                    Debug.WriteLine("Move item.");
                    return;
                case "f2":
                    //Rename current item.
                    RenameItem(null, null);
                    return;
                case "f8":
                    //Delete current item.
                    Debug.WriteLine("Delete item.");
                    return;
                case "backspace":
                    //Parent folder.
                    if(CurrentPath == "")
                    {
                        return;
                    }
                    OpenItem(new DirectoryItem("...", "", 0, side, false, ItemType.TopDir));
                    return;
                case "enter":
                case "numpadenter":
                    if (SelectedItems.Count != 1) return;
                    Debug.WriteLine(SelectedItems[0]);
                    OpenItem((Item)SelectedItems[0]);
                    return;
            }
        }

        public void RenameItem(Item item, string newName)
        {
            Debug.WriteLine("Changing name of item.");
        }


        public void Refresh()
        {
            Debug.WriteLine("Refresh.");
            if(CurrentPath == "")
            {
                FillDriveList();
                return;
            }
            DirectoryInfo d = new DirectoryInfo(CurrentPath);
            FillList(d);
        }

        public async Task<ImageSource> GetFileIcon(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                return ImageSource.FromResource("folder_icon.png");
            }
            string extension = Path.GetExtension(filePath);

            if (_fileIconCache.TryGetValue(extension, out var byteArray))
            {
                MemoryStream ms2 = new MemoryStream(byteArray);
                return new StreamImageSource { Stream = token => Task.FromResult<Stream>(new MemoryStream(byteArray)) };
            }

            // Otherwise, get the icon from the file, add it to the cache, and return it
            Icon rawIcon = await Task.Run(() => Icon.ExtractAssociatedIcon(filePath));
            Bitmap bitmap = rawIcon.ToBitmap();

            // Save the Bitmap to a MemoryStream
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            // Convert the MemoryStream to a byte array and add it to the cache
            _fileIconCache[extension] = ms.ToArray();

            // Reset the position of MemoryStream and create a StreamImageSource
            ms.Position = 0;
            return new StreamImageSource { Stream = token => Task.FromResult<Stream>(new MemoryStream(ms.ToArray())) };
        }

        void PathChanged(string value)
        {
            if (CurrentPath.Length == 0)
            {
                FillDriveList();
                return;
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(CurrentPath);
            if (_files == null)
            {

                // previousPath = _currentPath;
                return;
            }
            if (!Directory.Exists(directoryInfo.FullName))
            {
                CurrentPath = previousPath;
                return;
            }
            _files.Clear();
            FillList(directoryInfo);
        }


        public void OpenItem(Item item)
        {
            Debug.WriteLine("Testing - " + item.FilePath);
            if (item is DirectoryItem)
            {
                if (item.FileName == "...")
                {
                        IsLoading = true;

                        DirectoryInfo directoryInfo = Directory.GetParent(_currentPath);

                        if (directoryInfo == null)
                        {
                            FillDriveList();
                            return;
                        }

                        CurrentPath = directoryInfo.FullName;
                        previousPath = _currentPath;
                        _files.Clear();
                        FillList(directoryInfo);
                    return;
                }
                if (Directory.Exists(item.FilePath))
                {
                    IsLoading = true;
                    Debug.WriteLine("Exists");
                    // If the item is a folder, update the Files collection to show the contents of the folder
                    DirectoryInfo directoryInfo = new DirectoryInfo(item.FilePath);
                    CurrentPath = item.FilePath;
                    //previousPath = _currentPath;
                    _files.Clear();
                    FillList(directoryInfo);
                    return;
                }
            }
            else if (item is FileItem)
            {
                FileInfo fileInfo = new FileInfo(item.FilePath);
                Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
            }
        }

        private void FillDriveList()
        {
            _files.Clear();
            CurrentPath = "";
            previousPath = _currentPath;
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                Debug.WriteLine(drive.Name);
                string size = FileUtil.ConvertBytesToHumanReadable(drive.TotalFreeSpace) + " / " + FileUtil.ConvertBytesToHumanReadable(drive.TotalSize);
                _files.Add(new DriveItem(drive.Name + " - " + drive.VolumeLabel, drive.Name, side, size, (drive.DriveType == DriveType.Fixed ? "Drive" : drive.DriveType) + " --- " + drive.DriveFormat));
            }
            IsLoading = false;
        }

        private async Task FillList(DirectoryInfo d)
        {
            MainThread.BeginInvokeOnMainThread(() => IsLoading = true);
            await Task.Delay(100); //Is needed for the loading indicator to function.
            _files.Clear();
            _files.Add(new DirectoryItem("...", "", 0, side, false, ItemType.TopDir));
            try
            {

                ConcurrentBag<Item> fileSystemInfos = new ConcurrentBag<Item>();

                await Task.WhenAll(d.EnumerateDirectories().Select(async dir =>
                {
                    // It's a directory
                    DirectoryInfo dirInfo = new DirectoryInfo(dir.FullName);
                   // Debug.WriteLine(dir.FullName);

                    fileSystemInfos.Add(new DirectoryItem(dirInfo.Name, dirInfo.FullName, 0, side, (dir.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden), ItemType.Dir));
                }));

                await Task.WhenAll(d.EnumerateFiles().Select(async file =>
                {
                    FileInfo fileInfo = new FileInfo(file.FullName);
                    string size = FileUtil.ConvertBytesToHumanReadable(fileInfo.Length);


                    var icon = await GetFileIcon(fileInfo.FullName);

                    fileSystemInfos.Add(new FileItem(fileInfo.Name, fileInfo.FullName, size, fileInfo.Extension, icon, side, (file.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden)));
                }));

                foreach (var item in fileSystemInfos
                    .OrderBy(fsi => fsi is FileItem)
                    .ThenBy(fsi => fsi.FileName))
                {
                    _files.Add(item);
                }
                MainThread.BeginInvokeOnMainThread(() => IsLoading = false);
                previousPath = CurrentPath;
            }
            catch (UnauthorizedAccessException e)
            {
                //Check for no permission.
                DirectoryInfo directoryInfo = new DirectoryInfo(previousPath);
                CurrentPath = previousPath;
                _files.Clear();
                FillList(directoryInfo);

                //TODO: Popup here!!
                System.Diagnostics.Debug.WriteLine("No permission!");
            }
        }
    }
}
