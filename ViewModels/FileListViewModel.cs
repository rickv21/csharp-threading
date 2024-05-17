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
        private readonly object _filesLock = new object();
        private string _currentPath;
        private string _previousPath;
        private readonly ConcurrentDictionary<string, byte[]> _fileIconCache;
        private readonly short _side;
        private IList<object> _selectedItems;
        private readonly object _selectedItemsLock = new object();
        private bool _isLoading;

        private string _fileNameText;
        private string _infoText;
        private string _sizeText;
        private string _dateText;

        /// <summary>
        /// Gets or sets the collection of files, ensuring thread-safety using a lock.
        /// </summary>
        public ObservableCollection<Item> Files
        {
            get
            {
                lock (_filesLock)
                {
                    return _files;
                }
            }
            set
            {
                lock (_filesLock)
                {
                    _files = value;
                    OnPropertyChanged(nameof(Files));
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of selected items, ensuring thread-safety using a lock.
        /// </summary>
        public IList<object> SelectedItems
        {
            get
            {
                lock (_selectedItemsLock)
                {
                    return _selectedItems;
                }
            }
            set
            {
                lock (_selectedItemsLock)
                {
                    _selectedItems = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current path and notifies when the value changes.
        /// </summary>
        public string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
            }
        }

        /// <summary>
        /// Gets or sets the loading state and notifies when the value changes.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// Gets or sets the file name text and notifies when the value changes.
        /// </summary>
        public string FileNameText
        {
            get { return _fileNameText; }
            set { _fileNameText = value; OnPropertyChanged(nameof(FileNameText)); }
        }

        /// <summary>
        /// Gets or sets the info text and notifies when the value changes.
        /// </summary>
        public string InfoText
        {
            get { return _infoText; }
            set { _infoText = value; OnPropertyChanged(nameof(InfoText)); }
        }

        /// <summary>
        /// Gets or sets the size text and notifies when the value changes.
        /// </summary>
        public string SizeText
        {
            get { return _sizeText; }
            set { _sizeText = value; OnPropertyChanged(nameof(SizeText)); }
        }

        /// <summary>
        /// Gets or sets the date text and notifies when the value changes.
        /// </summary>
        public string DateText
        {
            get { return _dateText; }
            set { _dateText = value; OnPropertyChanged(nameof(DateText)); }
        }

        /// <summary>
        /// Command triggered when an item is double-tapped.
        /// </summary>
        public ICommand ItemDoubleTappedCommand { get; }

        /// <summary>
        /// Command triggered when the path is changed.
        /// </summary>
        public ICommand PathChangedCommand { get; }

        /// <summary>
        /// Command to sort files alphabetically.
        /// </summary>
        public ICommand SortFilesCommand { get; }

        /// <summary>
        /// Command to sort files based on size.
        /// </summary>
        public ICommand SortFilesOnSizeCommand { get; }

        /// <summary>
        /// Command to sort files based on date.
        /// </summary>
        public ICommand SortFilesOnDateCommand { get; }


        /// <summary>
        /// Initializes a new instance of the FileListViewModel class with the specified file icon cache and side.
        /// </summary>
        /// <param name="fileIconCache">The cache for file icons.</param>
        /// <param name="side">The side of the file explorer.</param>
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

        /// <summary>
        /// Renames the selected item with a new name and handles exceptions.
        /// </summary>
        /// <param name="selectedItem">The item to rename.</param>
        /// <param name="newName">The new name for the item.</param>
        /// <param name="extension">The extension for the item.</param>
        public static void RenameItem(Item selectedItem, string newName, string extension)
        {
            if (selectedItem != null)
            {
                string oldPath = selectedItem.FilePath;
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName + extension);
                try
                {
                    if (Directory.Exists(oldPath))
                    {
                        Directory.Move(oldPath, newPath);
                    }
                    else if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath);
                    }
                    selectedItem.FileName = newName + extension;
                }
                catch (IOException ex)
                {
                    Shell.Current.DisplayAlert("Error", "The given name already exists in the current folder.", "OK");
                    return;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Shell.Current.DisplayAlert("Error", "You do not have permission to rename this file.", "OK");
                    return;
                }
                catch (Exception ex)
                {
                    Shell.Current.DisplayAlert("Error", "An error occurred while renaming the file: " + ex.Message, "OK");
                    return;
                }

            }
            else
            {
                Shell.Current.DisplayAlert("No file selected", "Please select a file to rename", "");
            }
        }

        /// <summary>
        /// Checks if the files are sorted by date.
        /// </summary>
        /// <param name="files">The collection of files to check.</param>
        /// <returns>True if the files are sorted by date; otherwise, false.</returns>
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

        /// <summary>
        /// Sorts the files by date in ascending or descending order.
        /// </summary>
        /// <param name="files">The collection of files to sort.</param>
        /// <param name="isAscending">Whether to sort in ascending order.</param>
        /// <returns>The sorted collection of files.</returns>
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

        /// <summary>
        /// Sorts the files based on the date.
        /// </summary>
        private void SortFilesOnDate()
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileDates(_files, IsSortedOnDate(_files));
        }

        /// <summary>
        /// Checks if the files are sorted by size.
        /// </summary>
        /// <param name="files">The collection of files to check.</param>
        /// <returns>True if the files are sorted by size; otherwise, false.</returns>
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

        /// <summary>
        /// Sorts the files by size in ascending or descending order.
        /// </summary>
        /// <param name="files">The collection of files to sort.</param>
        /// <param name="isAscending">Indicates whether the sorting should be in ascending order.</param>
        /// <returns>A sorted collection of files.</returns>
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

        /// <summary>
        /// Sorts the files based on their size.
        /// </summary>
        private void SortFilesOnSize()
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileSizes(_files, IsSortedOnSize(_files));
        }

        /// <summary>
        /// Checks if the files are sorted alphabetically by a given label.
        /// </summary>
        /// <param name="files">The collection of files to check.</param>
        /// <param name="labelText">The label text to determine the sorting criteria.</param>
        /// <returns>True if the files are sorted alphabetically; otherwise, false.</returns>
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

        /// <summary>
        /// Gets the sorted items based on the specified label text.
        /// </summary>
        /// <param name="files">The collection of files to sort.</param>
        /// <param name="labelText">The label text to determine the sorting criteria.</param>
        /// <returns>A sorted collection of items.</returns>
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

        /// <summary>
        /// Sorts the file names alphabetically based on the specified label text.
        /// </summary>
        /// <param name="files">The collection of files to sort.</param>
        /// <param name="labelText">The label text to determine the sorting criteria.</param>
        /// <returns>A sorted collection of files.</returns>
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

        /// <summary>
        /// Sorts the files alphabetically based on the specified label text.
        /// </summary>
        /// <param name="labelText">The label text to determine the sorting criteria.</param>
        private void SortFilesAlphabetically(string labelText)
        {
            if (IsLoading)
            {
                return;
            }
            Files = SortFileNames(_files, labelText);
        }

        /// <summary>
        /// Handles various key press events to perform corresponding file operations.
        /// </summary>
        /// <param name="key">The key pressed by the user.</param>
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
                    RenameItem(null, null, null);
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
                    Task.Run(async () => await OpenItemAsync(new DirectoryItem("...", "", 0, _side, false, false, null, ItemType.TopDir)));
                    return;
                case "enter":
                case "numpadenter":
                    if (SelectedItems.Count != 1) return;
                    Debug.WriteLine(SelectedItems[0]);
                    Task.Run(async () => await OpenItemAsync((Item)SelectedItems[0]));
                    return;
            }
        }

        /// <summary>
        /// Refreshes the current file list asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Retrieves the icon of a file or folder as an image source.
        /// </summary>
        /// <param name="filePath">The path of the file or folder.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the image source.</returns>
        public async Task<ImageSource> GetFileIcon(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                return ImageSource.FromResource("folder_icon.png");
            }
            string extension = Path.GetExtension(filePath);

            if (_fileIconCache.TryGetValue(extension, out var byteArray))
            {
                MemoryStream ms2 = new(byteArray);
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

        /// <summary>
        /// Handles the path change event, updating the file list accordingly.
        /// </summary>
        /// <param name="value">The new path value.</param>
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
                await AppShell.Current.DisplayAlert("Invalid location", "This path does not exist, please try again", "OK");
                CurrentPath = _previousPath;
                return;
            }
            await Task.Run(async () => await FillList(directoryInfo));
        }

        /// <summary>
        /// Opens the specified item. If it's a directory, updates the file list; if it's a file, opens it.
        /// </summary>
        /// <param name="item">The item to open.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
                }
                else
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

        /// <summary>
        /// Fills the list of drives in the file explorer.
        /// </summary>
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

        /// <summary>
        /// Fills the list of files and directories in the specified directory.
        /// </summary>
        /// <param name="d">The directory to list the contents of.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

                    fileSystemInfos.Add(new DirectoryItem(dirInfo.Name, dirInfo.FullName, 0, _side, (dir.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden), FileUtil.IsSymbolicLink(dir.FullName), lastEditDirectory, ItemType.Dir));
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

                    fileSystemInfos.Add(new FileItem(fileInfo.Name, fileInfo.FullName, size, fileInfo.Extension, icon, _side, (file.Attributes & FileAttributes.Hidden) == (FileAttributes.Hidden), FileUtil.IsSymbolicLink(file.FullName), lastEdit));
                }));


                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Add(new DirectoryItem("...", "", 0, _side, false, false, null, ItemType.TopDir));
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

        /// <summary>
        /// Refreshes the list of files in the current directory.
        /// </summary>
        public async void RefreshFiles()
        {
            DirectoryInfo directoryInfo = new(CurrentPath);
            _files.Clear();
            await FillList(directoryInfo);
        }

        /// <summary>
        /// Deletes the selected items, moving them to the recycle bin.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Shows an error message box with the specified message and details.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="details">Additional details about the error.</param>
        private static void ShowErrorMessageBox(string message, string details)
        {
            MessageBox.Show($"{message}\n\nDetails: {details}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}