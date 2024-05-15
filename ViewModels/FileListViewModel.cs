using FileManager.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;
using System.Windows;
using System.Security;

namespace FileManager.ViewModels
{
    public class FileListViewModel : ViewModelBase
    {
        private ObservableCollection<Item> _files;
        private string _currentPath;
        private string _previousPath;
        private readonly ConcurrentDictionary<string, byte[]> _fileIconCache;
        private readonly short _side;
        private IList<object> _selectedItems;
        private bool _isLoading;

        private string _fileNameText;
        private string _infoText;
        private string _sizeText;
        private string _dateText;

        public ObservableCollection<Item> Files
        {
            get { return _files; }
            set
            {
                _files = value;
                OnPropertyChanged(nameof(Files));
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

        public string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string FileNameText
        {
            get { return _fileNameText; }
            set { _fileNameText = value; OnPropertyChanged(nameof(FileNameText)); }
        }

        public string InfoText
        {
            get { return _infoText; }
            set { _infoText = value; OnPropertyChanged(nameof(InfoText)); }
        }

        public string SizeText
        {
            get { return _sizeText; }
            set { _sizeText = value; OnPropertyChanged(nameof(SizeText)); }
        }

        public string DateText
        {
            get { return _dateText; }
            set { _dateText = value; OnPropertyChanged(nameof(DateText)); }
        }

        public ICommand ItemDoubleTappedCommand { get; }
        public ICommand PathChangedCommand { get; }
        public ICommand SortFilesCommand { get; }
        public ICommand SortFilesOnSizeCommand { get; }
        public ICommand SortFilesOnDateCommand { get; }

        public FileListViewModel(ConcurrentDictionary<string, byte[]> fileIconCache, short side)
        {
            _side = side;
            SortFilesCommand = new Command<string>(SortFilesAlphabetically);
            SortFilesOnSizeCommand = new Command(SortFilesOnSize);
            SortFilesOnDateCommand = new Command(SortFilesOnDate);
            Files = [];
            _fileIconCache = fileIconCache;
            _previousPath = "";
            FileNameText = "Filename";
            InfoText = "Info";
            SizeText = "Size";
            SelectedItems = [];
            ItemDoubleTappedCommand = new Command<Item>(async (item) => await OpenItemAsync(item));
            //Run when path textfield is changed in the UI.
            PathChangedCommand = new Command<string>(PathChanged);

            //Set the current path to the user's documents folder.
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ; DirectoryInfo d = new(_currentPath);


            Task.Run(async () => await FillList(d));
        }

        public string GetCurrentPath()
        {
            return CurrentPath;
        }

        public static void RenameItem(Item selectedItem, string newName)
        {
            string oldPath = selectedItem.FilePath;
            string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);
            if (Directory.Exists(oldPath))
            {
                Directory.Move(oldPath, newPath);
            }
            else if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }
            selectedItem.FileName = newName;
        }

        private static bool IsSortedOnDate(ObservableCollection<Item> files)
        {
            for (int i = 1; i < files.Count; i++)
            {
                if (files[i].LastEdited < files[i - 1].LastEdited)
                {
                    return true;
                }
            }

            return false;
        }

        private ObservableCollection<Item> SortFileDates(ObservableCollection<Item> files, bool isAscending)
        {
            ObservableCollection<Item> sortedItems = [];
            Item folder = files.FirstOrDefault(file => file.FileName.Equals("..."));
            if (files.Count > 1)
            {
                if (isAscending)
                {
                    sortedItems = new ObservableCollection<Item>(files
                      .Where(file => file.FileName != "...")
                      .OrderBy(file => file.LastEdited));
                    DateText = "Date v";
                }
                else
                {
                    sortedItems = new ObservableCollection<Item>(files
                     .Where(file => file.FileName != "...")
                     .OrderByDescending(file => file.LastEdited));
                    DateText = "Date ^";
                }
                FileNameText = "Filename";
                InfoText = "Info";
                SizeText = "Size";
            }
            else
            {
                sortedItems = new ObservableCollection<Item>(files
                    .Where(file => file.FileName != "..."));
            }

            if (folder != null)
            {
                sortedItems.Insert(0, folder);
            }

            return sortedItems;
        }

        private void SortFilesOnDate()
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileDates(_files, IsSortedOnDate(_files));
        }

        private static bool IsSortedOnSize(ObservableCollection<Item> files)
        {
            for (int i = 1; i < files.Count; i++)
            {
                if (files[i].Size < files[i - 1].Size)
                {
                    return true;
                }
            }

            return false;
        }

        private ObservableCollection<Item> SortFileSizes(ObservableCollection<Item> files, bool isAscending)
        {
            ObservableCollection<Item> sortedItems = [];
            Item folder = files.FirstOrDefault(file => file.FileName.Equals("..."));
            if (files.Count > 1)
            {
                if (isAscending)
                {
                    sortedItems = new ObservableCollection<Item>(files
                       .Where(file => file.FileName != "...")
                       .OrderBy(file => file.Size));
                    SizeText = "Size v";
                }
                else
                {
                    sortedItems = new ObservableCollection<Item>(files
                       .Where(file => file.FileName != "...")
                       .OrderByDescending(file => file.Size));
                    SizeText = "Size ^";
                }
                FileNameText = "Filename";
                InfoText = "Info";
                DateText = "Date";
            }
            else
            {
                sortedItems = new ObservableCollection<Item>(files
                    .Where(file => file.FileName != "..."));
            }
            FileNameText = "Filename";
            InfoText = "Info";
            DateText = "Date";
            
            if (folder != null)
            {
                sortedItems.Insert(0, folder);
            }

            return sortedItems;
        }

        private void SortFilesOnSize()
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileSizes(_files, IsSortedOnSize(_files));
        }

        private static bool IsSortedOnAlphabeticalLabel(ObservableCollection<Item> files, string labelText)
        {
            for (int i = 1; i < files.Count; i++)
            {
                if (labelText.Contains("Filename"))
                {
                    if (string.Compare(files[i].FileName, files[i - 1].FileName, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        return true;
                    }
                }
                else if (labelText.Contains("Info"))
                {
                    if (string.Compare(files[i].FileInfo, files[i - 1].FileInfo, StringComparison.InvariantCultureIgnoreCase) < 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private ObservableCollection<Item> GetSortedItems(ObservableCollection<Item> files, string labelText)
        {
            bool isSorted = IsSortedOnAlphabeticalLabel(files, labelText);
            if (files.Count <= 1)
            {
                return new ObservableCollection<Item>(files
                      .Where(file => file.FileName != "..."));
            }

            if (!isSorted)
            {
                DateText = "Date";
                SizeText = "Size";
                if (labelText.Contains("Filename"))
                {
                    FileNameText = "Filename ^";
                    InfoText = "Info";
                    return new ObservableCollection<Item>(files
                        .Where(file => file.FileName != "...")
                        .OrderByDescending(file => file.FileName));
                }
                else if (labelText.Contains("Info"))
                {
                    InfoText = "Info ^";
                    FileNameText = "Filename";
                    return new ObservableCollection<Item>(files
                        .Where(file => file.FileName != "...")
                        .OrderByDescending(file => file.FileInfo));
                }
            }
            else
            {
                DateText = "Date";
                SizeText = "Size";
                if (labelText.Contains("Filename"))
                {
                    FileNameText = "Filename v";
                    InfoText = "Info";
                    return new ObservableCollection<Item>(files
                        .Where(file => file.FileName != "...")
                        .OrderBy(file => file.FileName));
                }
                else if (labelText.Contains("Info"))
                {
                    InfoText = "Info v";
                    FileNameText = "Filename";
                    return new ObservableCollection<Item>(files
                        .Where(file => file.FileName != "...")
                        .OrderBy(file => file.FileInfo));
                }
            }
            // Should be unreachable
            throw new Exception("Something went wrong");
        }

        private ObservableCollection<Item> SortFileNames(ObservableCollection<Item> files, string labelText)
        {
            bool isSorted = IsSortedOnAlphabeticalLabel(files, labelText);
            Item folder = files.FirstOrDefault(file => file.FileName.Equals("..."));

            ObservableCollection<Item> sortedItemsExcludingFolder = GetSortedItems(files, labelText);

            if (folder != null)
            {
                sortedItemsExcludingFolder.Insert(0, folder);
            }

            return sortedItemsExcludingFolder;
        }

        private void SortFilesAlphabetically(string labelText)
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileNames(_files, labelText);
        }

        public void HandleClick(string key)
        {
            if (IsLoading)
            {
                //Failsafe, allows the user to cancel the loading process in case it gets stuck.
                if (key == "escape")
                {
                    IsLoading = false;
                    CurrentPath = _previousPath;
                    Task.Run(RefreshAsync);
                }
                return;
            }
            switch (key)
            {
                case "f5":
                    //Refresh
                    Task.Run(RefreshAsync);
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
                    if (CurrentPath == "")
                    {
                        return;
                    }
                    Task.Run(async () => await OpenItemAsync(new DirectoryItem("...", "", 0, _side, false, null, ItemType.TopDir)));
                    return;
                case "enter":
                case "numpadenter":
                    if (SelectedItems.Count != 1) return;
                    Debug.WriteLine(SelectedItems[0]);
                    Task.Run(async () => await OpenItemAsync((Item)SelectedItems[0]));
                    return;
            }
        }

        public async Task RefreshAsync()
        {
            Debug.WriteLine("Refresh.");
            if (CurrentPath == "")
            {
                FillDriveList();
                return;
            }
            DirectoryInfo d = new(CurrentPath);
            await FillList(d);
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
            MemoryStream ms = new();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            // Convert the MemoryStream to a byte array and add it to the cache
            _fileIconCache[extension] = ms.ToArray();

            // Reset the position of MemoryStream and create a StreamImageSource
            ms.Position = 0;
            return new StreamImageSource { Stream = token => Task.FromResult<Stream>(new MemoryStream(ms.ToArray())) };
        }

        public async void PathChanged(string value)
        {
            if (CurrentPath.Length == 0)
            {
                FillDriveList();
                return;
            }
            DirectoryInfo directoryInfo = new(CurrentPath);
            if (_files == null)
            {
                //Debug to check if this is ever hit.
                Debug.Assert(false, "Files is null in PathChanged.");
                return;
            }
            if (!Directory.Exists(directoryInfo.FullName))
            {
                AppShell.Current.DisplayAlert("Invalid location", "This path does not exist!", "OK");
                CurrentPath = _previousPath;
                return;
            }
            Task.Run(async () => await FillList(directoryInfo));
        }


        public async Task OpenItemAsync(Item item)
        {
            Debug.WriteLine("Testing - " + item.FilePath);
            if (item is DirectoryItem)
            {
                if (item.FileName == "...")
                {
                    DirectoryInfo directoryInfo = Directory.GetParent(CurrentPath);

                    if (directoryInfo == null)
                    {
                        FillDriveList();
                        return;
                    }

                    CurrentPath = directoryInfo.FullName;
                    _previousPath = CurrentPath;
                    await FillList(directoryInfo);
                    return;
                }
                if (Directory.Exists(item.FilePath))
                {
                    Debug.WriteLine("Exists");
                    // If the item is a folder, update the Files collection to show the contents of the folder
                    DirectoryInfo directoryInfo = new(item.FilePath);
                    CurrentPath = item.FilePath;
                    await FillList(directoryInfo);
                    return;
                } else
                {
                    await AppShell.Current.DisplayAlert("Invalid location", "This folder does not exist anymore!", "OK");
                    await RefreshAsync();
                    return;
                }
            }
            else if (item is FileItem)
            {
                FileInfo fileInfo = new(item.FilePath);
                Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
            }
        }

        private void FillDriveList()
        {
            if (IsLoading)
            {
                return;
            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = true;
                Files.Clear();
                CurrentPath = "";
                _previousPath = CurrentPath;
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in allDrives)
                {
                    // Check if this is needed
                    string size = FileUtil.ConvertBytesToHumanReadable(drive.TotalSize - drive.TotalFreeSpace) + " / " + FileUtil.ConvertBytesToHumanReadable(drive.TotalSize);
                    //string size = FileUtil.ConvertBytesToHumanReadable(drive.TotalFreeSpace) + " / " + FileUtil.ConvertBytesToHumanReadable(drive.TotalSize);
                    Files.Add(new DriveItem(drive.Name + " - " + drive.VolumeLabel, drive.Name, _side, size, (drive.DriveType == DriveType.Fixed ? "Drive" : drive.DriveType) + " --- " + drive.DriveFormat, null));
                }
                IsLoading = false;
            });
        }

        private async Task FillList(DirectoryInfo d)
        {
            if (IsLoading)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = true;
                Files.Clear();
                SelectedItems.Clear();
                FileNameText = "Filename";
                InfoText = "Info";
                SizeText = "Size";
                DateText = "Date";
            });
            await Task.Delay(100); //Is needed for the loading indicator to function.
            try
            {

                ConcurrentBag<Item> fileSystemInfos = [];

                await Task.WhenAll(d.EnumerateDirectories().Select(async dir =>
                {
                    // It's a directory
                    DirectoryInfo dirInfo = new(dir.FullName);
                    DateTime lastEditDirectory = DateTime.MinValue;
                    if (dirInfo.LastWriteTime != DateTime.MinValue)
                    {
                        lastEditDirectory = dirInfo.LastWriteTime;
                    }

                    fileSystemInfos.Add(new DirectoryItem(dirInfo.Name, dirInfo.FullName, 0, _side, (dir.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden), lastEditDirectory, ItemType.Dir));
                }));

                await Task.WhenAll(d.EnumerateFiles().Select(async file =>
                {
                    FileInfo fileInfo = new(file.FullName);
                    long size = fileInfo.Length;
                    DateTime lastEdit = DateTime.MinValue;
                    if (fileInfo.LastWriteTime != DateTime.MinValue)
                    {
                        lastEdit = fileInfo.LastWriteTime;
                    }

                    var icon = await GetFileIcon(fileInfo.FullName);

                    fileSystemInfos.Add(new FileItem(fileInfo.Name, fileInfo.FullName, size, fileInfo.Extension, icon, _side, (file.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden), lastEdit));
                }));


                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Add(new DirectoryItem("...", "", 0, _side, false, null, ItemType.TopDir));
                    foreach (var item in fileSystemInfos.OrderBy(fsi => fsi is FileItem).ThenBy(fsi => fsi.FileName))
                    {
                        Files.Add(item);
                    }

                    IsLoading = false;
                    _previousPath = CurrentPath;
                });


            }
            catch (UnauthorizedAccessException e)
            {
                await Shell.Current.DisplayAlert("No permission", "You do not have permission to access this folder.", "OK");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                });
                DirectoryInfo directoryInfo = new(_previousPath);
                CurrentPath = _previousPath;
                await FillList(directoryInfo);
            }
        }

        public async void RefreshFiles()
        {
            DirectoryInfo directoryInfo = new(CurrentPath);
            _files.Clear();
            await FillList(directoryInfo);
        }

        public async Task DeleteItem()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
            {
                Debug.WriteLine("No items selected.");
                // No items selected, return early
                return;
            }

            var itemsToDelete = SelectedItems.Cast<Item>().ToList();

            foreach (var item in itemsToDelete)
            {
                try
                {
                    // Show a confirmation MessageBox
                    Task<bool> result = Shell.Current.DisplayAlert("Confirmation", "Are you sure you want to delete " + item.FileName + "? ", "OK", "Cancel");

                    if (await result)
                    {
                        if (item is FileItem fileItem)
                        {
                            // Delete the file
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fileItem.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                        else if (item is DirectoryItem directoryItem)
                        {
                            // Delete the directory
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(directoryItem.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }

                        // Remove the item from the Files collection
                        Files.Remove(item);
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = ex switch
                    {
                        ArgumentException => "The path is a zero-length string, is malformed, contains only white space, or contains invalid characters (including wildcard characters). The path is a device path (starts with \\\\.\\).",
                        DirectoryNotFoundException => "The directory does not exist or is a file.",
                        IOException => "A file in the directory or subdirectory is in use.",
                        NotSupportedException => "The directory name contains a colon (:).",
                        SecurityException => "The user does not have required permissions.",
                        OperationCanceledException => "The user cancels the operation or the directory cannot be deleted.",
                        _ => "An unexpected error occurred while deleting the item.",
                    };
                    ShowErrorMessageBox(errorMessage, ex.Message);

                }

                // Clear the SelectedItems collection
                SelectedItems.Clear();
            }
        }

        private static void ShowErrorMessageBox(string message, string details)
        {
            MessageBox.Show($"{message}\n\nDetails: {details}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}