using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using FileManager.Models;
using FileManager.Views.Popups;
using Application = Microsoft.Maui.Controls.Application;

namespace FileManager.ViewModels;

public class FileOverviewViewModel : ViewModelBase
{
    public static int MAX_THREADS = 255;

    private FileListViewModel _leftSideViewModel;
    public FileListViewModel LeftSideViewModel
    {
        get { return _leftSideViewModel; }
        set
        {
            _leftSideViewModel = value;
            OnPropertyChanged(nameof(LeftSideViewModel));
        }
    }

    private FileListViewModel _rightSideViewModel;
    public FileListViewModel RightSideViewModel
    {
        get { return _rightSideViewModel; }
        set
        {
            _rightSideViewModel = value;
            OnPropertyChanged(nameof(RightSideViewModel));
        }
    }

    private readonly ConcurrentDictionary<string, byte[]> _fileIconCache = new();

    private int _activeSide;

    public int ActiveSide
    {
        get { return _activeSide; }
        set
        {
            _activeSide = value;
        }
    }

    private IEnumerable<Item> _droppedFiles;
    public IEnumerable<Item> DroppedFiles
    {
        get => _droppedFiles;
        set
        {
            _droppedFiles = value;
            OnPropertyChanged(nameof(DroppedFiles));
        }
    }

    // Constructor 
    private CollectionView _leftCollection;

    private CollectionView _rightCollection;

    public FileOverviewViewModel(CollectionView leftCollection, CollectionView rightCollection)
    {
        _leftCollection = leftCollection;
        _rightCollection = rightCollection;

        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
    }

    public FileOverviewViewModel()
    {
        LeftSideViewModel = new FileListViewModel(_fileIconCache, 0);
        RightSideViewModel = new FileListViewModel(_fileIconCache, 1);
        ActiveSide = 0;
    }

    public async Task<string> SelectActionAsync()
    {
        return await Application.Current.MainPage.DisplayActionSheet("Select Action", "Cancel", null, "Copy", "Paste", "Move");
    }

    public async Task<(string, string?)> PromptUserAsync(string action, bool isDir = false)
    {
        string number = await Application.Current.MainPage.DisplayPromptAsync("Enter Number", $"Number of threads for {action}:", "OK", "Cancel", "0", maxLength: 10, keyboard: Microsoft.Maui.Keyboard.Numeric);
        if (int.Parse(number) > MAX_THREADS || int.Parse(number) < 1)
        {
            return (number, null);
        }

        if (isDir)
        {
            string directoryName = await Application.Current.MainPage.DisplayPromptAsync("Enter regex", $"Please enter a regex to select files to {action}:", "OK", "Cancel", null, maxLength: 100);

            return (number, directoryName);
        }

        return (number, null);
    }

    public async Task ProcessActionAsync(string action, int number, string regex, ContentPage view, IList<Item> items, List<object> selectedItems, string targetPath)
    {
        var fileCount = 0;
        foreach (var item in items)
        {
            fileCount += CountFiles(item);
        }
        var popup = new PopupPage();
        view.ShowPopup(popup);
        for (var i = 0; i <= fileCount; i++)
        {
            var label = popup.Content.FindByName("Label") as Label;
            label.Text = $"Files done:{i}/{fileCount}";
            label.TextColor = Colors.Black;
            var progressBar = popup.FindByName("ProgressBar") as ProgressBar;
            progressBar.Progress = (double)(((double)i / (double)fileCount) * 1);
            switch (action)
            {
                case "Move":
                    await Task.Delay(1000);
                    break;
                case "Copy":
                    await Task.Delay(2000);
                    await CopyItems(selectedItems, number);
                    break;
                case "Paste":
                    await Task.Delay(2000);
                    PasteItems(targetPath);
                    break;
            }
        }
        popup.Close();
    }


    public void PassClickEvent(string key)
    {
        Debug.WriteLine("Pass click event " + ActiveSide);
        if (ActiveSide == 0)
        {
            LeftSideViewModel.HandleClick(key);
        }
        else
        {
            RightSideViewModel.HandleClick(key);
        }
    }


    public void UpdateSelected(IList<object> leftSelectedItems, IList<object> rightSelectedItems)
    {
        LeftSideViewModel.SelectedItems = leftSelectedItems;
        RightSideViewModel.SelectedItems = rightSelectedItems;
    }

    public string GetCurrentPath(CollectionView collectionView)
    {
        if (collectionView == _leftCollection)
        {
            return LeftSideViewModel.CurrentPath;
        }
        else if (collectionView == _rightCollection)
        {
            return RightSideViewModel.CurrentPath;
        }
        else
        {
            return null;
        }
    }

    private List<string> _copiedFilesPaths = new List<string>();
    public List<string> CopiedFilesPaths
    {
        get { return _copiedFilesPaths; }
        set { _copiedFilesPaths = value; }
    }

    private string tempCopyDirectory = Path.Combine(Path.GetTempPath(), "FileManagerCopiedItems");

    /// <summary>
    /// Threading: Locks en Task Parallel Library (TPL)
    /// 
    /// For copying multiple files, a combination of locking and the Task Parallel Library (TPL) is used.
    /// The lock ensures that only one thread at a time can access the _copiedFilesPaths list to prevent race conditions.
    /// The TPL is used to perform the copying of each individual file or directory in parallel on the ThreadPool using tasks.
    /// This can lead to better performance, especially when copying many files or large files.
    /// 
    /// TPL is not the same as threadpool. It provides higher-level constructs for parallel programming, while ThreadPool is a low-level mechanism for managing threads.
    /// </summary>
    public async Task CopyItems(List<object> selectedItems, int maxDegreeOfParallelism)
    {
        // Lock to ensure thread safety when modifying shared resources
        lock (_copiedFilesPaths)
        {
            // Clear the list of copied files paths
            _copiedFilesPaths.Clear();

            // Ensure the temporary directory exists
            if (!Directory.Exists(tempCopyDirectory))
            {
                Directory.CreateDirectory(tempCopyDirectory);
            }
        }

        // Create a dictionary to store the thread IDs
        var threadIds = new ConcurrentDictionary<int, bool>();

        await Task.Run(() =>
        {
            // TPL with specific degree of parallelism
            Parallel.ForEach(selectedItems, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item =>
            {
                // Get the current thread ID
                int threadId = Thread.CurrentThread.ManagedThreadId;

                // Add the thread ID to the dictionary if it doesn't exist
                threadIds.TryAdd(threadId, true);

                if (item is FileItem fileItem) // If file
                {
                    string fileName = Path.GetFileName(fileItem.FilePath);
                    string tempFilePath = Path.Combine(tempCopyDirectory, fileName);

                    // Perform file copy synchronously
                    File.Copy(fileItem.FilePath, tempFilePath, true);

                    // Lock within the synchronous method
                    lock (_copiedFilesPaths)
                    {
                        _copiedFilesPaths.Add(tempFilePath);
                    }
                }
                else if (item is DirectoryItem directoryItem) // If directory
                {
                    string dirName = Path.GetFileName(directoryItem.FilePath);
                    string tempDirPath = Path.Combine(tempCopyDirectory, dirName);

                    // Perform directory copy synchronously
                    DirectoryCopy(directoryItem.FilePath, tempDirPath, true);

                    // Lock within the synchronous method
                    lock (_copiedFilesPaths)
                    {
                        _copiedFilesPaths.Add(tempDirPath);
                    }
                }
            });
        });

        // Print the number of threads used
        Debug.WriteLine($"Number of threads used: {threadIds.Count}");
    }

    /// <summary>
    /// Threading: Threadpool
    ///  
    /// This method recursively copies a directory to a new location, copying files and subdirectories in parallel using .NET's ThreadPool.
    /// By using Parallel.ForEach, .NET's underlying ThreadPool is used to distribute iterations across files and directories across multiple threads.
    /// This makes effective use of available CPU cycles and can lead to better performance, especially when copying large amounts of data or using slow storage media.
    /// </summary>
    /// <param name="sourceDirPath"></param>
    /// <param name="destDirPath"></param>
    /// <param name="copySubDirs"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Use ThreadPool to copy files and directories
        var copyTasks = new List<Task>();

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var copyTask = new Task(() =>
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            });

            // Queue the task to the ThreadPool
            ThreadPool.QueueUserWorkItem(_ => copyTask.Start());
            copyTasks.Add(copyTask);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                var copyTask = new Task(() =>
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                });

                // Queue the task to the ThreadPool
                ThreadPool.QueueUserWorkItem(_ => copyTask.Start());
                copyTasks.Add(copyTask);
            }
        }

        // Wait for all copy tasks to complete
        Task.WaitAll(copyTasks.ToArray());
    }

    /// <summary>
    /// Threading: locks
    /// 
    /// For when multiple files are pasted or perhaps files are copied. By the lock 
    /// only 1 file or directory has access to the _copiedFilesPaths list. Here, multiple sources cannot
    /// edit the list.
    /// </summary>
    public void PasteItems(string targetPath)
    {
        lock (_copiedFilesPaths)
        {
            foreach (var sourcePath in _copiedFilesPaths)
            {
                string fileName = Path.GetFileName(sourcePath);
                string destFilePath = Path.Combine(targetPath, fileName);

                if (File.Exists(sourcePath))
                {
                    if (File.Exists(destFilePath))
                    {
                        MessageBox.Show("File already exists in target path: " + destFilePath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Copy file
                    File.Copy(sourcePath, destFilePath, true);
                }
                else if (Directory.Exists(sourcePath))
                {
                    if (Directory.Exists(destFilePath))
                    {
                        MessageBox.Show("Directory already exists in target path: " + destFilePath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Copy directory
                    DirectoryCopy(sourcePath, destFilePath, true);
                }
            }

            // Refresh
            RightSideViewModel.RefreshFiles();
            LeftSideViewModel.RefreshFiles();

            // empty copy list
            _copiedFilesPaths.Clear();
        }
    }

    private void DirectoryMove(string sourceDirPath, string destDirPath)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirPath);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDirPath);

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirPath, file.Name);
            file.MoveTo(tempPath);
        }

        foreach (DirectoryInfo subdir in dirs)
        {
            string tempPath = Path.Combine(destDirPath, subdir.Name);
            DirectoryMove(subdir.FullName, tempPath);
        }

        Directory.Delete(sourceDirPath, false);
    }

    private int CountFiles(Item item)
    {
        if (item.Type != ItemType.Dir)
        {
            return 1;
        }
        var dir = item as DirectoryItem;
        return dir.ItemCount;
    }
}